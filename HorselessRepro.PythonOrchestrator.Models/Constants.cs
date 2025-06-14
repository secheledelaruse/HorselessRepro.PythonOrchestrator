﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorselessRepro.PythonOrchestrator.Models
{
    public static class Constants
    {
        public const string CosmosResultsUnavailableMessage = "cosmos result unavailable";
        public const string NoMessagesInQueueMessage = "no messages in queue";

        public const string CosmosContainerEntries = "entries";
        public const string CosmosContainerPyEntries = "pyentries";

        public const string ReproBlobContainerName = "reprocontainer";
        public const string ReproBlobName = "reproblob.txt";
        public const string PythonReproBlobName = "pythonreproblob.txt";

        public const string CosmosEndpointAccountEndpoint = "CosmosEndpointConfig__AccountEndpoint";
    }
}
