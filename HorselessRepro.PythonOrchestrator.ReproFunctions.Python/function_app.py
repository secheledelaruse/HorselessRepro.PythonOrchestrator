import logging
import azure.functions as func
from cosmosfuncs import bp
import os

app = func.FunctionApp()
app.register_functions(bp)
 
print ("environment:")
print(os.environ)


@app.timer_trigger(schedule="*/5 * * * * *", arg_name="myTimer", run_on_startup=False,
              use_monitor=False) 
def timer_trigger(myTimer: func.TimerRequest) -> None:
    if myTimer.past_due:
        logging.info('The timer is past due!')

    logging.info('Python timer trigger function executed.')

# add storage queue trigger
@app.queue_trigger(arg_name="myQueueItem", queue_name="myqueue-items", connection="AzureWebJobsStorage")
@app.queue_output(arg_name="outputQueueItem", queue_name="passthrough", connection="AzureWebJobsStorage")
def queue_trigger(myQueueItem: func.QueueMessage, outputQueueItem: func.Out[str]) -> None:
    message = myQueueItem.get_body().decode("utf-8")
    logging.info(f'Python queue trigger function processed item: {message}')
    # Write the message to the passthrough queue
    outputQueueItem.set(message)
    logging.info(f'Queue item content: {message}')
