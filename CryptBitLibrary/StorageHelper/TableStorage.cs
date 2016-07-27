using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace CryptBitLibrary.Storage
{

    public class TableStorage<T> where T : TableEntity, new()
    {

        CloudStorageAccount storageAccount = StorageHelper.storageAccount;
        CloudTableClient tableClient;
        CloudTable table;

        public static Dictionary<string, bool> tableExists = new Dictionary<string, bool>();

        public TableStorage(string tableName)
        {
            // Create the table client.
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference(tableName);

            // Create the table if it doesn't exist and keep reference in a local static collection so we're not hitting table storage all the time.
            if (!tableExists.ContainsKey(tableName))
            {
                try
                {
                    tableExists.Add(tableName, true);
                } catch (Exception ex)
                {
                    //Concurrency error
                }
                table.CreateIfNotExists();                                               
            }

        }

        //INSERT operations


        public void InsertOrReplaceBatch(List<T> entities)
        {
            TableBatchOperation batchCreateOperation = new TableBatchOperation();

            int x = 0;
            foreach (T entity in entities)
            {
                x++;
                batchCreateOperation.Add(TableOperation.InsertOrReplace(entity));

                if (batchCreateOperation.Count() >= 100 || x >= entities.Count)
                {
                    table.ExecuteBatch(batchCreateOperation);
                    batchCreateOperation.Clear();
                }
            }

        }
        public T InsertOrReplace(T entity)
        {
            TableOperation insertOperation = TableOperation.InsertOrReplace(entity);
            table.Execute(insertOperation);
            return entity;
        }

        public T Insert(T entity)
        {
            TableOperation insertOperation = TableOperation.Insert(entity);
            table.Execute(insertOperation);
            return entity;
        }

        public T InsertOrMerge(T entity)
        {
            TableOperation insertOperation = TableOperation.InsertOrMerge(entity);
            table.Execute(insertOperation);
            return entity;
        }


        //GET OPERATIONS
        public List<T> GetAll()
        {
            List<T> entities = null;
            if (entities == null || entities.Count == 0)
            {
                TableQuery<T> query = new TableQuery<T>();
                entities = table.ExecuteQuery(query).ToList();
            }
            return entities;
        }
        public List<T> GetAllForPartition(string partitionKey)
        {

            List<T> entities = new List<T>();

            if (entities.Count == 0)
            {
                TableQuery<T> query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
                entities = table.ExecuteQuery(query).ToList();
            }
            return entities;
        }
        public T GetSingleByRowKey(string rowkey)
        {
            //Use carefully, this causes a table scan and will take forever for anything more than a few entities in a table
            T entity = null;

            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowkey));

            var tableSet = table.ExecuteQuery(query).ToList();

            if (tableSet.Count >= 1)
            {
                entity = tableSet.First();
            }

            return entity;

        }

        public T GetSingleByFilterProperty(string property, string value)
        {
            //Use carefully, this causes a table scan and will take forever for anything more than a few entities in a table
            T entity = null;

            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition(property, QueryComparisons.Equal, value));

            var tableSet = table.ExecuteQuery(query).ToList();

            if (tableSet.Count >= 1)
            {
                entity = tableSet.First();
            }

            return entity;
        }

        public T GetSingle(string partitionKey, string rowKey)
        {

            string key = string.Format("{0}:{1}:{2}", typeof(T).Name, partitionKey, rowKey);

            T entity = null;

            TableQuery<T> query = new TableQuery<T>().Where(
                TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey)));

            var tableSet = table.ExecuteQuery(query).ToList();

            if (tableSet.Count >= 1)
            {
                entity = tableSet.First();
            }


            return entity;
        }

        //DELETE operations
        public void Delete(T entity)
        {
            TableOperation insertOperation = TableOperation.Delete(entity);
            table.Execute(insertOperation);

        }
    }
}
