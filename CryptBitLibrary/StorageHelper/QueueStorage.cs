using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace CryptBitLibrary.Storage
{
    public class QueueStorage<T>
    {
        CloudStorageAccount storageAccount = StorageHelper.storageAccount;
        CloudQueueClient queueClient;
        CloudQueue queue;
        public static Dictionary<string, bool> queueExists = new Dictionary<string, bool>();

        public Nullable<int> approximateMessageCount
        {
            get
            {
                return queue.ApproximateMessageCount;
            }
        }
        public QueueStorage(string queuename)
        {
            queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference(queuename);

            if (!queueExists.ContainsKey(queuename))
            {
                try
                {
                    queueExists.Add(queuename, true);
                }
                catch (Exception ex)
                {
                    //Concurrency error
                }
                queue.CreateIfNotExists();
            }
        }

        public CloudQueueMessage EnqueueMessage(T message)
        {
            CloudQueueMessage cloudMessage = new CloudQueueMessage(CommonHelper.SerializeToJson(message));
            queue.AddMessage(cloudMessage);
            return cloudMessage;
        }

        public CloudQueueMessage DequeueMessage(TimeSpan ttl)
        {
            return queue.GetMessage(ttl);
        }

        public void DeleteMessage(CloudQueueMessage message)
        {
            queue.DeleteMessage(message);
        }

        public void UpdateMessageTTL(CloudQueueMessage message, TimeSpan ttl)
        {
            queue.UpdateMessage(message, ttl, MessageUpdateFields.Visibility);
        }



    }
}
