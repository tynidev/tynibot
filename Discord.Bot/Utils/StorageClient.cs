using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Discord.Bot.Utils;
using Newtonsoft.Json;

namespace Discord.Bot
{
    public class StorageClient : IDisposable
    {
        private readonly TableServiceClient TableServiceClient;
        private readonly Dictionary<string, TableClient> TableClients;

        public StorageClient(string connectionString)
        {
            this.TableServiceClient = new TableServiceClient(connectionString);
            this.TableClients = new Dictionary<string, TableClient>();
        }

        public void Dispose()
        {
        }

        public async Task<T> GetTableRow<T>(string tableName, string rowKey, string partitionKey = "default")
            where T : ValueWithEtag
        {
            TableClient tableClient = await this.GetTableClient(tableName);

            try
            {
                var res = await tableClient.GetEntityAsync<ContentDataEntity>(partitionKey, rowKey);

                T value = JsonConvert.DeserializeObject<T>(res.Value.Content);
                value.etag = res.Value.ETag;
                return value;
            }
            catch(RequestFailedException)
            {
                return null;
            }
        }

        public async Task<List<T>> GetAllRowsAsync<T>(string tableName, string partitionKey) 
            where T : ValueWithEtag
        {
            TableClient tableClient = await this.GetTableClient(tableName);

            try
            {
                var res = await tableClient.QueryAsync<ContentDataEntity>((item) => string.Equals(item.PartitionKey, partitionKey)).ToListAsync();

                return res.ConvertAll(item => {
                        T value = JsonConvert.DeserializeObject<T>(item.Content);
                        value.etag = item.ETag;
                        return value;
                    });
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }


        public async Task SaveTableRow<T>(string tableName, string rowKey, string partitionKey, T entity)
            where T : ValueWithEtag
        {
            TableClient tableClient = await this.GetTableClient(tableName);

            ContentDataEntity entry = new ContentDataEntity
            {
                RowKey = rowKey,
                PartitionKey = partitionKey,
                Content = JsonConvert.SerializeObject(entity)
            };

            if (entity.etag == default)
            {
                await tableClient.UpsertEntityAsync(entry);
            }
            else
            {
                var res = await tableClient.UpdateEntityAsync(entry, entity.etag);
            }
        }

        public async Task SaveTableRows<T>(string tableName, IEnumerable<(string, T)> entitiesAndRowKey, string partitionKey)
        {
            TableClient tableClient = await this.GetTableClient(tableName);
            List<TableTransactionAction> actions = new List<TableTransactionAction>();
            
            foreach ((string rowKey, T entity) in entitiesAndRowKey)
            {
                ContentDataEntity entry = new ContentDataEntity
                {
                    RowKey = rowKey,
                    PartitionKey = partitionKey,
                    Content = JsonConvert.SerializeObject(entity)
                };

                actions.Add(new TableTransactionAction(TableTransactionActionType.UpsertMerge, entry));
            }

            await tableClient.SubmitTransactionAsync(actions);
        }

        public async Task ExecuteTransaction<T>(string tableName, IEnumerable<(string, TableTransactionActionType, T, ETag)> transactionActions, string partitionKey)
            where T : ValueWithEtag
        {
            TableClient tableClient = await this.GetTableClient(tableName);

            List<TableTransactionAction> actions = new List<TableTransactionAction>();
            foreach ((string rowKey, TableTransactionActionType actionType, T entity, ETag etag) in transactionActions)
            {
                ContentDataEntity entry = new ContentDataEntity
                {
                    RowKey = rowKey,
                    PartitionKey = partitionKey,
                    Content = JsonConvert.SerializeObject(entity)
                };

                actions.Add(new TableTransactionAction(actionType, entry, etag));
            }

            await tableClient.SubmitTransactionAsync(actions);
        }

        public async Task DeleteTableRow(string tableName, string rowKey, string partitionKey = "default", ETag etag = default)
        {
            TableClient tableClient = await this.GetTableClient(tableName);

            try
            {
                await tableClient.DeleteEntityAsync(partitionKey, rowKey, etag);
            }
            catch (RequestFailedException)
            {
            }
        }

        private async Task<TableClient> GetTableClient(string tableName)
        {
            if (!this.TableClients.TryGetValue(tableName, out TableClient tableClient))
            {
                tableClient = this.TableServiceClient.GetTableClient(tableName);
                await tableClient.CreateIfNotExistsAsync();

                this.TableClients.TryAdd(tableName, tableClient);

                return tableClient;
            }

            return tableClient;
        }
    }
}
