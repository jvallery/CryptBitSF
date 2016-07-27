using CryptBitLibrary;
using CryptBitLibrary.DataEntities;
using CryptBitLibrary.Storage;
using Microsoft.Azure.KeyVault;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace CryptBitWeb.Controllers
{
    public class ArchiveController : ApiController
    {

        private TableStorage<Archive> archiveClient = new TableStorage<Archive>("Archives");
        private QueueStorage<string> processQueue = new QueueStorage<string>("processarchive");

        // GET: api/Archive
        public async Task<string> Get()
        {

            Archive a = new Archive();

            try
            {
                a.archiveKey = await CommonHelper.CreateKey(a.RowKey);
                archiveClient.Insert(a);
                return a.RowKey;
            }
            catch (Exception ex)
            {
                Logger.TrackException(ex, 0, "Error inserting new archive");
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

        }

        // GET: api/Archive/5
        public Archive Get(string id, Boolean status)
        {

            Archive a;
            try
            {
                a = archiveClient.GetSingle(id.Substring(0, 2), id);
            }
            catch (Exception ex)
            {
                Logger.TrackException(ex, 0, "Error getting item from table storage");
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (a == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return a;

        }


        public HttpResponseMessage Get(string id)
        {
            HttpResponseMessage resp = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            Archive a;
            try
            {
                a = archiveClient.GetSingle(id.Substring(0, 2), id);


                if (a == null || a.status != 3)
                {
                    resp = Request.CreateResponse(HttpStatusCode.NotFound);
                }

                if (a.status == 3)
                {

                    using (MemoryStream ms = new MemoryStream())
                    {

                        string zipFileName = string.Format("{0}.zip", a.RowKey);

                        KeyVaultKeyResolver cloudResolver = new KeyVaultKeyResolver(CommonHelper.GetToken);
                        BlobEncryptionPolicy policy = new BlobEncryptionPolicy(null, cloudResolver);
                        BlobRequestOptions options = new BlobRequestOptions() { EncryptionPolicy = policy };


                        CloudBlobClient client = StorageHelper.storageAccount.CreateCloudBlobClient();
                        CloudBlobContainer container = client.GetContainerReference("archives");

                        CloudBlockBlob blob = container.GetBlockBlobReference(zipFileName);

                        blob.DownloadToStream(ms, null, options, null);
                        ms.Seek(0, SeekOrigin.Begin);

                        resp = Request.CreateResponse(HttpStatusCode.OK);
                        resp.Content = new ByteArrayContent(ms.ToArray());
                        resp.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                        resp.Content.Headers.ContentDisposition.FileName = zipFileName;
                        resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
                        
                        return resp;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.TrackException(ex, 0, "Error getting item from table storage");
                resp = Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            return resp;
        }




        // POST: api/Archive
        // PUT: api/Archive/5
        //Finalize once all items are complete
        public string Put(string id)
        {

            try
            {
                Archive a = archiveClient.GetSingle(id.Substring(0, 2), id);

                if (a.status == 0)
                {
                    a.status = 1;
                    a.statusText = "Upload compete. Processing enqueued.";
                    archiveClient.InsertOrMerge(a);
                    processQueue.EnqueueMessage(a.RowKey);
                    return a.RowKey;

                }
                else
                {
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
                }

            }
            catch (Exception ex)
            {
                Logger.TrackException(ex, 0, "Error setting item in table storage");
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

        }

        // DELETE: api/Archive/5
        public void Delete(string id)
        {
            Archive a;
            try
            {
                a = archiveClient.GetSingle(id.Substring(0, 2), id);
            }
            catch (Exception ex)
            {
                Logger.TrackException(ex, 0, "Error getting item from table storage");
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (a != null)
            {
                archiveClient.Delete(a);
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
        }

    }
}
