-- Create the Master Key with a password.
-- CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'Internet5&';

-- CREATE DATABASE SCOPED CREDENTIAL tim_UAMI_test
--     WITH IDENTITY = 'Managed Identity'

-- EXEC sys.sp_enable_event_stream

-- EXEC sys.sp_create_event_stream_group
--     @stream_group_name =      N'myStreamGroupTest',
--     @destination_type =       N'AzureEventHubsAmqp',
--     @destination_location =   N'metsoft.servicebus.windows.net/kulahub',
--     @destination_credential = tim_UAMI_test,
--     @max_message_size_kb =    256,
--     @partition_key_scheme =   N'None'

-- EXEC sys.sp_add_object_to_event_stream_group
--     N'myStreamGroupTest',
--     N'dbo.Contacts'

CREATE DATABASE SCOPED CREDENTIAL TimTest2ForCES
    WITH IDENTITY = 'SHARED ACCESS SIGNATURE',
    SECRET = 'SharedAccessSignature sr=https%3a%2f%2fmetsoft.servicebus.windows.net%2fkulahub&sig=VJdr8tn30XsLQHMXdBOVSdYzHdPdUCwGFibfU51cnFc%3d&se=1776010219&skn=TestCES' --Be sure to copy the entire token. The SAS token starts with "SharedAccessSignature sr="

--EXEC sys.sp_enable_event_stream

EXEC sys.sp_create_event_stream_group
    @stream_group_name =      N'myStreamGroupTest',
    @destination_type =       N'AzureEventHubsAmqp',
    @destination_location =   N'metsoft.servicebus.windows.net/kulahub',
    @destination_credential = TimTest2ForCES,
    @max_message_size_kb =    256,
    @partition_key_scheme =   N'None'

EXEC sys.sp_add_object_to_event_stream_group
    N'myStreamGroupTest',
    N'dbo.Contacts'