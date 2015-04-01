#load "../../packages/MBrace.Azure.Client/bootstrap.fsx"

namespace global

[<AutoOpen>]
module ConnectionStrings = 

    open MBrace
    open MBrace.Azure
    open MBrace.Azure.Client
    open MBrace.Azure.Runtime

    let myStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=cloudmbracestorage;AccountKey=U78eeobMZLKTI+YDTsYTaF+ltPvWFDGjoThO+FcXVpDkisBB1OaGlmDMZH7MjE9t3vRhpbiP/e+e60HkRh1yBw=="
    let myServiceBusConnectionString = "Endpoint=sb://cloudmbracens.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=1Hw6Ui28qK99V4FVPMMVgOwgQG4bw9IGdMd6MBho6iw="

    let createStorageConnectionString(storageName, storageAccessKey) = sprintf "DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=%s" storageName storageAccessKey
    let createServiceBusConnectionString(serviceBusName, serviceBusKey) = sprintf "Endpoint=sb://%s.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=%s" serviceBusName serviceBusKey

    let config =
        { Configuration.Default with
            StorageConnectionString = myStorageConnectionString
            ServiceBusConnectionString = myServiceBusConnectionString }

    
