Dropzone.autoDiscover = false;
//Dropzone.options.dropzoneForm = false;


$("div#dropzoneForm").dropzone({

    url: "/api/file",
    autoProcessQueue: false,
    uploadMultiple: false,
    parallelUploads: 100,
    maxFiles: 100,
    archiveId: 0,

    init: function () {

        var submitButton = document.querySelector("#submit-all");
        var wrapperThis = this;

        submitButton.addEventListener("click", function () {
            $("button#submit-all").hide();
            $.getJSON("/api/archive",
            function (data) {
                wrapperThis.archiveId = data;
                wrapperThis.options.url = "/api/file/" + wrapperThis.archiveId;
                wrapperThis.processQueue();
            });            
        });




        this.on('queuecomplete', function (data, resp) {

            $.ajax({
                type: "PUT",
                url: "/api/archive/" + wrapperThis.archiveId,
                complete: function () {
                    $("div#dropZoneContainer").hide();
                    
                    $("div#archiveDownloadPending").show();

                    (function poll() {
                  
                            $.ajax({
                                url: "/api/archive/" + wrapperThis.archiveId + "?status=true",
                                type: "GET",
                                success: function (data) {
                                    
                                    $("div#archiveDownloadStatus").html(data.statusText)
                                    if (data.status != 3) {
                                        //Not completed -- so recurse
                                        setTimeout(poll, 1000);
                                    } else {
                                        //Download complete so show the URL
                                        $("div#archiveDownloadPending").hide();
                                        $("div#archiveDownloadComplete").show();
                                        

                                        pathArray = location.href.split('/');
                                        protocol = pathArray[0];
                                        host = pathArray[2];
                                        url = protocol + '//' + host;
                                        downloadLink = url + '/api/archive/' + wrapperThis.archiveId 

                                        $('<a href="' + downloadLink + '">' + downloadLink + '</a>').appendTo($('div#archiveDownloadUrl'));
                                    }
                                },
                                dataType: "json"                                                                
                            })                  
                    })();

                }
            });
         
        });
    }
});

