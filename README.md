# HorselessRepro.PythonOrchestrator

this is an aspire app with python and c# functions

it's meant to exercise connecting the containerized python functions and the non-containerized c# functions to the same storage emulator

the aim is to get rid of the below error
```
fail: Host.Startup[0]
      The listener for function 'Functions.timer_trigger' was unable to start.
      Microsoft.Azure.WebJobs.Host.Listeners.FunctionListenerException: The listener for function 'Functions.timer_trigger' was unable to start.
       ---> System.AggregateException: Retry failed after 6 tries. Retry settings can be adjusted in ClientOptions.Retry or by configuring a custom retry policy in ClientOptions.RetryPolicy. (Connection refused (127.0.0.1:10000)) (Connection refused (127.0.0.1:10000)) (Connection refused (127.0.0.1:10000)) (Connection refused (127.0.0.1:10000)) (Connection refused (127.0.0.1:10000)) (Connection refused (127.0.0.1:10000))
       ---> Azure.RequestFailedException: Connection refused (127.0.0.1:10000)
       ---> System.Net.Http.HttpRequestException: Connection refused (127.0.0.1:10000)
       ---> System.Net.Sockets.SocketException (111): Connection refused
         at System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs.ThrowException(SocketError error, CancellationToken cancellationToken)
         at System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token)
         at System.Net.Sockets.Socket.<ConnectAsync>g__WaitForConnectWithCancellation|285_0(AwaitableSocketAsyncEventArgs saea, ValueTask connectTask, CancellationToken cancellationToken)
         at System.Net.Http.HttpConnectionPool.ConnectToTcpHostAsync(String host, Int32 port, HttpRequestMessage initialRequest, Boolean async, CancellationToken cancellationToken)
         --- End of inner exception stack trace ---
         at System.Net.Http.HttpConnectionPool.ConnectToTcpHostAsync(String host, Int32 port, HttpRequestMessage initialRequest, Boolean async, CancellationToken cancellationToken)
         at System.Net.Http.HttpConnectionPool.ConnectAsync(HttpRequestMessage request, Boolean async, CancellationToken cancellationToken)
         at System.Net.Http.HttpConnectionPool.CreateHttp11ConnectionAsync(HttpRequestMessage request, Boolean async, CancellationToken cancellationToken)
         at System.Net.Http.HttpConnectionPool.AddHttp11ConnectionAsync(QueueItem queueItem)
         at System.Threading.Tasks.TaskCompletionSourceWithCancellation`1.WaitWithCancellationAsync(CancellationToken cancellationToken)
         at System.Net.Http.HttpConnectionPool.SendWithVersionDetectionAndRetryAsync(HttpRequestMessage request, Boolean async, Boolean doRequestAuth, CancellationToken cancellationToken)
         at System.Net.Http.HttpClient.<SendAsync>g__Core|83_0(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationTokenSource cts, Boolean disposeCts, CancellationTokenSource pendingRequestsCts, CancellationToken originalCancellationToken)

```
