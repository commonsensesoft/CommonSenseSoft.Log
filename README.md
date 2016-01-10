# CommonSenseSoft.Log
.NET Logging library, polished for over a decade by developer eating his own dog food. Anything useful to port troubleshooting fast and make it as useful and resilient as possible.

To Use this library you need files from bin directory only.
Just reference this library from your .NET project, initialize the settings at start of your application and you are good to go.

The full explanation of the advantages is yet to come when time allows. To list a few for now:

1. The Logging class is static, which means that you do not need to initialize it in every class.
2. Has option of local log level, so you can use that to selectively debug parts of application by just change in configuration.
3. For most used Exception types goes deeper to output as much info as possible.
4. Fall-back log file in case it can't write to the configured Log. Will also write why it failed to write to the designated file.
5. Limits on log file size and notifications about approaching that limit.
6. Separate daily log file option.
7. Email notifications throttling so that mailbox is not flooded with the same notification generated every second.
8. Fall-back SMTP servers option with primary server failure warnings appended to the notifications.
9. Option to log assemblies engaged by the process. Indispensable to troubleshoot issues related to versioning.
10. Can monitor your Application Memory footprint.
11. 

It is not as fast-writing as other loggers, so use it where you need fast to implement, easy but sophisticated and resilient logging, but are not expecting to write millions of records per second.