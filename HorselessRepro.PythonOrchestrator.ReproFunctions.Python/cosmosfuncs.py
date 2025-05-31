import azure.functions as func
import logging
from azure.cosmos import CosmosClient, PartitionKey
from azure.storage.blob import BlobServiceClient
from datetime import datetime
import os

bp = func.Blueprint() 
@bp.function_name(name="CosmosDBTrigger")

@bp.cosmos_db_trigger(arg_name="documents", 
                       connection="cosmosdb",
                       database_name="reprodb", 
                       container_name="entries", 
                       lease_container_name="leases",
                       create_lease_container_if_not_exists="true")
def cosmos_db_triggered(documents: func.DocumentList) -> None:
    if documents:
        logging.info('Document id: %s', documents[0]['id'])
        # Initialize Cosmos client
        endpoint = "<COSMOS_DB_ENDPOINT>"
        key = "<COSMOS_DB_KEY>"
        client = CosmosClient(endpoint, key)
        database_name = "reprodb"
        container_name = "pyentries"
        database = client.get_database_client(database_name)
        container = database.get_container_client(container_name)
        # Write all input documents to the output collection
        for doc in documents:
            container.upsert_item(doc.to_json()) 

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

