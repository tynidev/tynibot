using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

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

        public async Task<T> GetTableRow<T>(string tableName, string rowKey, string paritionKey = "default")
            where T : class, ITableEntity, new()
        {
            TableClient tableClient = await this.GetTableClient(tableName);

            try
            {
                var res = await tableClient.GetEntityAsync<T>(paritionKey, rowKey);

                return res.Value;
            }
            catch(RequestFailedException e)
            {
                return null;
            }
        }

        public async Task AddTableRow<T>(string tableName, T entity)
        where T : class, ITableEntity, new()
        {
            TableClient tableClient = await this.GetTableClient(tableName);

            await tableClient.UpsertEntityAsync<T>(entity);
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
