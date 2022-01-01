using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
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
        {
            TableClient tableClient = await this.GetTableClient(tableName);

            try
            {
                var res = await tableClient.GetEntityAsync<ContentDataEntity>(partitionKey, rowKey);
                
                return JsonConvert.DeserializeObject<T>(res.Value.Content);
            }
            catch(RequestFailedException e)
            {
                return default(T);
            }
        }

        public async Task<List<T>> GetAllRowsAsync<T>(string tableName, string partitionKey)
        {
            TableClient tableClient = await this.GetTableClient(tableName);

            try
            {
                var res = await tableClient.QueryAsync<ContentDataEntity>((item) => string.Equals(item.PartitionKey, partitionKey)).ToListAsync();

                return res.ConvertAll<T>(item => JsonConvert.DeserializeObject<T>(item.Content));
            }
            catch (RequestFailedException e)
            {
                return null;
            }
        }


        public async Task SaveTableRow<T>(string tableName, string rowKey, string partitionKey, T entity)
        {
            TableClient tableClient = await this.GetTableClient(tableName);

            ContentDataEntity entry = new ContentDataEntity
            {
                RowKey = rowKey,
                PartitionKey = partitionKey,
                Content = JsonConvert.SerializeObject(entity)
            };

            await tableClient.UpsertEntityAsync(entry);
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

        public async Task DeleteTableRow(string tableName, string rowKey, string partitionKey = "default")
        {
            TableClient tableClient = await this.GetTableClient(tableName);

            try
            {
                await tableClient.DeleteEntityAsync(partitionKey, rowKey);
            }
            catch (RequestFailedException e)
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
