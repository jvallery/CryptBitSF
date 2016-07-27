using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CryptBitLibrary.Storage
{
    public static class StorageHelper
    {

        public static CloudStorageAccount storageAccount;
        static StorageHelper()
        {

            storageAccount = CloudStorageAccount.Parse(CommonHelper.GetSetting("StorageConnectionString"));

        }

        public static bool UploadMessageMimeBlob(byte[] message, string hash)
        {
            bool success = false;

            try
            {
                string containerName = "blobs";
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(hash);
                blockBlob.UploadFromByteArray(message, 0, message.Length);
                success = true;
            }
            catch (Exception ex)
            {
               // InsertError(ex, 0, "Error uploading blob");
            }


            return success;
        }

        public static CloudBlockBlob DownloadMessageMimeBlob(string filename, string userid)
        {
            try
            {
                string containerName = "blobs";
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(filename);


                return blockBlob;

            }
            catch (Exception ex)
            {
              //  InsertError(ex, 0, "Error downloading blob");
            }


            return null;

        }

        public static string MD5(byte[] data)
        {
            MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bs = data; 
             bs = x.ComputeHash(bs);
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            return s.ToString();
        }


    }
}
