using CryptBitLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using CryptBitLibrary.Storage;
using CryptBitLibrary.DataEntities;
using Microsoft.Azure.KeyVault;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Web.Http;

namespace CryptBitWeb.Controllers
{
    public class FileController : ApiController
    {
        private TableStorage<Archive> archiveClient = new TableStorage<Archive>("Archives");


        // POST api/files/archiveID
        public async Task<HttpResponseMessage> Post(string id)
        {
            //Get the archive object
            Archive a = archiveClient.GetSingle(id.Substring(0, 2), id);

            //Get the RSA Key from KeyVault and setup the encryption policy
            var key = await CommonHelper.ResolveKey(a.archiveKey);
            BlobEncryptionPolicy policy = new BlobEncryptionPolicy(key, null);
            BlobRequestOptions options = new BlobRequestOptions() { EncryptionPolicy = policy };

            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);

          

            CloudBlobClient client = StorageHelper.storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(a.RowKey);
            container.CreateIfNotExists();
            try
            {
                foreach (var file in provider.Contents)
                {
                    if (!string.IsNullOrEmpty(file.Headers.ContentDisposition.FileName) && file.Headers.ContentLength > 0)
                    {
                        string fileName = file.Headers.ContentDisposition.FileName.Replace("\"", string.Empty);
                        CloudBlockBlob blob = container.GetBlockBlobReference(fileName);
                        blob.Properties.ContentType = file.Headers.ContentType.MediaType;

                        using (var dataStream = await file.ReadAsStreamAsync())
                        {
                            await blob.UploadFromStreamAsync(dataStream, dataStream.Length, null, options, null);
                        }
                    }

                }
            } catch (Exception ex)
            {
                Logger.TrackException(ex, 0, "Error uploading blob");
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        


        }
    }
}

