# NET5-Microservices-Using-MassTransit
Example: .NET 5 Microservices using MassTransit. It is easy to use and migrate from RabbitMQ to AmazonSQS and SNS for each environments.

    + RabbitMQ
        - Direct Queue(queue:consumer-consumers-order)
        - Exchange Topic: notification
            - Topic=email: will be received by EmailService
            - Topic=push: will be received by Notification
    + Amazon SQS and SNS
        - Direct Queue(queue:consumer-consumers-order)(Topic:Messages_Commands-Order), create a queue and create a SNS Topic which link the queue
        - Topic:Messages_Commands-INotification, Routing key: "push" and "email"

### Requirements
+ Install .NET5
+ Install Docker & Docker Compose
    - RabbitMQ Server Dashboard(http://localhost:15672/)

### Structures
+ Producer: using the API sends messages
+ Consumer: receive messages from Queue(consumer-consumers-order)
+ Notification: receive messages with Exchange=notification and Topic=push
+ EmailService: receive message with Exchange=notification and Topic=email
+ Messages: create message model and Service Connection Config for RabbitMQ and AmazonSQS

### Overview
+ Direct Queue

    ![Direct Queue](./Images/Direct-Queue.png)


+ Topic

    ![Topic](./Images/Topic.png)

### Usages

#### RabbitMQ Server
+ Start RabbitMQ Server
    ```
    cd DemoMicroservices
    docker-compose up
    ```
+ Start multi apps with Visual Studio

+ Run Producer: http://localhost:22270/swagger/index.html
    + Send a message to Queue(consumer-consumers-order), Run post Order API
        ```
        http://localhost:22270/Order
        ```

    + Send a message to Email Service, run post Notification API
        ```
        http://localhost:22270/Notification

        {
            "notificationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "notificationType": "email",
            "notificationContent": "email body",
            "notificationAddress": "nhat.thai@example.com",
            "notificationDate": "2021-12-06T04:31:16.654Z"
        }
        ```

    + Send a message to Notification, run post Notification API
        ```
        http://localhost:22270/Notification

        {
            "notificationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "notificationType": "push",
            "notificationContent": "body",
            "notificationAddress": "",
            "notificationDate": "2021-12-06T04:31:16.654Z"
        }
        ```
#### Amazon SQS and SNS
+ Create a user in IAM: add SQSFullAccess and SNSFullAccess
+ Set and add Region, AccessKey and SecretKey into MessageBusSQS in appsettings.json
    ```
    "ConnectionStrings": {
        "MessageBus": "amqp://admin:password@localhost:5671/",
        "MessageBusSQS": "[AccessKey]:[SecretKey]@[region]"
    },
    "UsingAmazonSQS":"true"
    ```

##### Direct Message
+ Amazon Queue(Direct Queue:consumer-consumers-order)

    ![Direct-Queue](./Images/SQS-Queue.jpg)

+ Create topic SNS
    Endpoint: arn:aws:sqs:ap-southeast-1:783560535431:consumer-consumers-order

    ![Topic with SQS Queue](./Images/Topic-SNS-withSQS.png)

+ Monitor SQS

    ![Monitor-Queue](./Images/Monitor-SQS.jpg)

##### Topic
+ Amazon Topic SNS

    ![Topic-SNS-To-Queues](./Images/Topic-SNS-To-Queues.png)

+ Subcription Topic with SQS, and set RoutingKey

    ![Subscriptions-Topic](./Images/Subscription-Topic-WithSQS.png)

### References
+ [Setting Up MassTransit and RabbitMQ](https://wrapt.dev/blog/building-an-event-driven-dotnet-application-setting-up-masstransit-and-rabbitmq)
+ [RabbitMQ with ASP.NET Core – Microservice Communication with MassTransit](https://codewithmukesh.com/blog/rabbitmq-with-aspnet-core-microservice/)
+ [MassTransit a real use case](https://blexin.com/en/blog-en/masstransit-a-real-use-case/)