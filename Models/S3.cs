using Amazon.S3;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MediatorTelegramBot.Models;

public class S3Client
{
    public AmazonS3Client S3;

    public S3Client(IConfiguration config)
    {
        S3 = new(new BasicAWSCredentials(config.GetConnectionString("S3:accesKey"), config.GetConnectionString("S3:secretKey")),
            new AmazonS3Config()
            {
                ServiceURL = "https://s3.1zq.ru",
                ForcePathStyle = true,
                AuthenticationRegion = "ru-irk-1"
            });
    }
}
