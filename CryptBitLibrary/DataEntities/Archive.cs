using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptBitLibrary.DataEntities
{
    public class Archive : TableEntity
    {
        public string md5 { get; set; }

        public int status { get; set; }

        public string statusText { get; set;  }
        public int size { get; set; }

        public string archiveKey { get; set; }

        public Archive()
        {
            RowKey = Guid.NewGuid().ToString();
            PartitionKey = RowKey.Substring(0, 2);
            statusText = "Not finalized";
            status = 0;
            


        }


    }
}
