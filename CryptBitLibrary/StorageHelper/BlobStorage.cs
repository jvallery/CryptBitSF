using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CryptBitLibrary.Storage
{
    public class BlobStorage
    {

        CloudStorageAccount storageAccount = StorageHelper.storageAccount;
        CloudBlobClient blobClient;
        CloudBlobContainer container;
        private int retryCount = 0;

        public BlobStorage(string containername)
        {
            // Create the blob client.
            blobClient = storageAccount.CreateCloudBlobClient();
            container = blobClient.GetContainerReference(containername);
            container.CreateIfNotExists();

            retryCount = 2;
        }

        
        public CloudBlockBlob UploadBlob(byte[] data, string filename, bool compressed = true, bool overwrite = false, string contentType = "application/octet-stream")
        {
            return UploadBlob(retryCount, data, filename, compressed, overwrite, contentType);
        }

        private CloudBlockBlob UploadBlob(int retry, byte[] data, string filename, bool compressed = true, bool overwrite = false, string contentType = "application/octet-stream")
        {
            CloudBlockBlob blob = null;
            try
            {
                blob = container.GetBlockBlobReference(filename);

                if (!blob.Exists() || overwrite)
                {
                    if (compressed)
                    {
                        using (MemoryStream comp = new MemoryStream())
                        {
                            using (GZipStream gzip = new GZipStream(comp, CompressionLevel.Optimal))
                            {
                                gzip.Write(data, 0, data.Length);
                                gzip.Close();
                            }

                            comp.Close();
                            data = comp.ToArray();
                        }
                    }

                    if (blob.Metadata.ContainsKey("compressed"))
                    {
                        blob.Metadata["compressed"] = compressed.ToString();
                    }
                    else
                    {
                        blob.Metadata.Add("compressed", compressed.ToString());
                    }

                    blob.Properties.ContentType = contentType;
                    blob.UploadFromByteArray(data, 0, data.Length);
                }


            }
            catch (StorageException ex)
            {
                if (retry >= 1)
                {
                    retry--;
                    return UploadBlob(retry, data, filename, compressed, overwrite, contentType);
                }
                else
                {
                    Logger.TrackException(ex, 30, string.Format("Error uploading blob {0} to container {1}", filename, container.Name));
                }
            }
            return blob;
        }

        public byte[] DownloadBlob(string filename)
        {
            try
            {
                CloudBlockBlob blob = container.GetBlockBlobReference(filename);
                byte[] data = null;
                blob.DownloadToByteArray(data, 0);
                blob.FetchAttributes();

                if (data != null && Convert.ToBoolean(blob.Metadata["compressed"]))
                {
                    using (MemoryStream comp = new MemoryStream(data))
                    {
                        using (MemoryStream decomp = new MemoryStream())
                        {
                            using (GZipStream gzip = new GZipStream(comp, CompressionMode.Decompress))
                            {
                                gzip.CopyTo(decomp);
                                gzip.Close();
                            }
                            decomp.Close();
                            data = decomp.ToArray();
                        }
                        comp.Close();
                    }
                }

                return data;

            }
            catch (Exception ex)
            {
                Logger.TrackException(ex, 31, string.Format("Error downloading blob {0} to container {1}", filename, container.Name));
            }

            return null;
        }



    }
}
