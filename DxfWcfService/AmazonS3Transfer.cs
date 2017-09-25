using Amazon.S3;
using Amazon;
using Amazon.Runtime;
using Amazon.S3.Transfer;
using Amazon.S3.Model;

namespace DxfWcfService
{
    public class AmazonS3Transfer
    {
        public bool sendMyFileToS3(System.IO.Stream localFilePath, string bucketName, string subDirectoryInBucket, string fileNameInS3, string accessKey, string secretKey)
        {
            //accessKey = @"AKIAI7YZWDTYKEIMVQKA";
            //secretKey = @"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";
            //bucketName = @"eshow001";

            BasicAWSCredentials creds = new BasicAWSCredentials(@accessKey, @secretKey);
            IAmazonS3 client = new AmazonS3Client(creds, RegionEndpoint.USEast1);
            //IAmazonS3 client = new AmazonS3Client(RegionEndpoint.USEast1);
            TransferUtility utility = new TransferUtility(client);
            TransferUtilityUploadRequest request = new TransferUtilityUploadRequest();
            if (subDirectoryInBucket == "" || subDirectoryInBucket == null)
            {
                request.BucketName = bucketName; //no subdirectory just bucket name  
            }
            else
            {   // subdirectory and bucket name  
                request.BucketName = bucketName + @"/" + subDirectoryInBucket;
            }
            request.Key = fileNameInS3; //file name up in S3  
            request.InputStream = localFilePath;
            utility.Upload(request); //commensing the transfer  

            return true; //indicate that the file was sent  
        }
        public bool getMyFileFromS3(string bucketName, string fileNameInS3, string localPath, string accessKey, string secretKey)
        {
            //accessKey = @"AKIAJJGNJEP5JIXCBLJA";
            //secretKey = @"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";
            //bucketName = @"eshow001";

            BasicAWSCredentials creds = new BasicAWSCredentials(@accessKey, @secretKey);
            IAmazonS3 client = new AmazonS3Client(creds, RegionEndpoint.USEast1);
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = fileNameInS3
            };
            GetObjectResponse response = client.GetObject(request);
            response.WriteResponseStreamToFile(localPath);
            return true;
        }
    }
}