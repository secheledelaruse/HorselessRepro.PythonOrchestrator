import azure.functions as func
import logging
from azure.cosmos import CosmosClient, PartitionKey
from azure.storage.blob import BlobServiceClient
from datetime import datetime
import os
from todoitem import ToDoItem
import json

bp = func.Blueprint() 
@bp.function_name(name="CosmosDBTrigger") 
@bp.cosmos_db_trigger(arg_name="documents", 
                       connection="cosmosdb",
                       database_name="reprodb", 
                       container_name="entries", 
                       lease_container_name="leases",
                       create_lease_container_if_not_exists="true")
@bp.cosmos_db_output(arg_name="savedDocuments", 
                      database_name="reprodb",
                      container_name="pyentries",
                      lease_container_name="leases",
                      create_if_not_exists=True,
                      connection="cosmosdb")     
def cosmos_db_triggered(documents: func.DocumentList, savedDocuments: func.Out[func.Document]) -> None:
    if documents:
        output_docs = []
        for doc in documents:
            try:
                doc_dict = dict(doc)
                todo = ToDoItem(**doc_dict)
                logging.info(f"not skipping todo document: {todo.Description}")
                output_docs.append(todo)
            except Exception as e:
                logging.info(f"Skipping non-TodoItem document: {doc}, reason: {e}")
        if output_docs:
            outItem = output_docs[0]
            now = datetime.utcnow().isoformat()
            outItem.Description = f"python cosmosdb trigger updated at {now}: {outItem.Description}"
            savedDocuments.set(func.Document.from_dict(outItem.to_dict()))


@bp.function_name(name="PythonBlobTrigger")
@bp.blob_trigger(arg_name="myblob",
                 path="reprocontainer/reproblob.txt",
                 connection="AzureWebJobsStorage")
def python_blob_triggered(myblob: func.InputStream) -> None:
    logging.info(f"Blob trigger function processed blob: {myblob.name}, Size: {myblob.length} bytes")
    # Read the blob content
    content = myblob.read().decode('utf-8')
    # Append update string with current UTC time
    updated_content = f"python modified {content} - updated by python function {datetime.utcnow().isoformat()}"
    # Save to a new blob
    blob_connection_str = os.environ["AzureWebJobsStorage"]
    blob_service_client = BlobServiceClient.from_connection_string(blob_connection_str)
    container_client = blob_service_client.get_container_client("reprocontainer")
    # Overwrite or create pythonreproblob.txt
    container_client.upload_blob(
        name="pythonreproblob.txt",
        data=updated_content,
        overwrite=True
    )

