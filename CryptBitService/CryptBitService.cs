using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using CryptBitLibrary.Storage;
using CryptBitLibrary.DataEntities;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.Azure.KeyVault;
using CryptBitLibrary;
using System.IO;
using System.IO.Compression;

namespace CryptBitService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class CryptBitService : StatelessService
    {
        public CryptBitService(StatelessServiceContext context)
            : base(context)
        {

            var configPkg = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            foreach (var setting in configPkg.Settings.Sections["CryptBitConfig"].Parameters)
            {
                CommonHelper.SetSetting(setting.Name, setting.Value);
            }

        }



        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            long iterations = 0;

            QueueStorage<string> processQueue = new QueueStorage<string>("processarchive");
            TableStorage<Archive> archiveClient = new TableStorage<Archive>("Archives");

            while (!cancellationToken.IsCancellationRequested)
            {

                try
                {

                    CloudQueueMessage message = processQueue.DequeueMessage(TimeSpan.FromMinutes(10));

                    if (message != null)
                    {
                        string archiveId = message.AsString.Trim('"');

                        Archive a = archiveClient.GetSingle(archiveId.Substring(0, 2), archiveId);

                        a.status = 2;
                        a.statusText = "Processing started.";
                        archiveClient.InsertOrMerge(a);

                        KeyVaultKeyResolver cloudResolver = new KeyVaultKeyResolver(CommonHelper.GetToken);
                        BlobEncryptionPolicy policy = new BlobEncryptionPolicy(null, cloudResolver);
                        BlobRequestOptions options = new BlobRequestOptions() { EncryptionPolicy = policy };


                        CloudBlobClient client = StorageHelper.storageAccount.CreateCloudBlobClient();
                        CloudBlobContainer container = client.GetContainerReference(a.RowKey);


                        using (var archiveStream = new MemoryStream())
                        {
                            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
                            {
                                foreach (IListBlobItem item in container.ListBlobs(null, false))
                                {
                                    CloudBlockBlob blob = (CloudBlockBlob)item;

                                    var archiveFile = archive.CreateEntry(blob.Name, CompressionLevel.Optimal);

                                    using (var entryStream = archiveFile.Open())
                                    {
                                        blob.DownloadToStream(entryStream, null, options, null);
                                        a.statusText = string.Format("Processing {0}", blob.Name);
                                        archiveClient.InsertOrMerge(a);
                                        Console.WriteLine(blob.Name);
                                    }
                                }
                            }


                            var key = CommonHelper.ResolveKey(a.archiveKey).Result;
                            policy = new BlobEncryptionPolicy(key, null);
                            options = new BlobRequestOptions() { EncryptionPolicy = policy };


                            CloudBlobContainer zipContainer = client.GetContainerReference("archives");
                            CloudBlockBlob zipBlob = zipContainer.GetBlockBlobReference(string.Format("{0}.zip", a.RowKey));
                            zipBlob.Properties.ContentType = "application/zip";

                            archiveStream.Seek(0, SeekOrigin.Begin);

                            zipBlob.UploadFromStream(archiveStream, archiveStream.Length, null, options, null);

                            //Delete the original container with all of the uploaded files
                            container.Delete();

                            a.status = 3;
                            a.statusText = "Processing complete.";
                            archiveClient.InsertOrMerge(a);
                        }
                        processQueue.DeleteMessage(message);
                    }


                }
                catch (Exception ex)
                {
                    Logger.TrackException(ex, 0, "Error in worker role");
                }


                cancellationToken.ThrowIfCancellationRequested();
                ServiceEventSource.Current.ServiceMessage(this, "Working-{0}", ++iterations);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }




        }

    }
}
