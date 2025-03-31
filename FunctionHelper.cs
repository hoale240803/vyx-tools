using Amazon.S3.Transfer;
using Amazon.S3;
using ClosedXML.Excel;
using Amazon;
using System.Text;
using Stripe;
using File = System.IO.File;
namespace GenerateImageName
{
    public static class FunctionHelper
    {
        private static async Task CleanUpBucketAsync(IAmazonS3 s3Client, string bucketName)
        {
            var listResponse = await s3Client.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request
            {
                BucketName = bucketName
            });

            if (listResponse.S3Objects.Count > 0)
            {
                var deleteRequest = new Amazon.S3.Model.DeleteObjectsRequest
                {
                    BucketName = bucketName,
                    Objects = listResponse.S3Objects.Select(o => new Amazon.S3.Model.KeyVersion { Key = o.Key }).ToList()
                };

                await s3Client.DeleteObjectsAsync(deleteRequest);
                Console.WriteLine("🗑️ Bucket cleaned up.");
            }
        }
        public static async Task UploadImageToS3Async()
        {
            string localRootFolder = @"C:\Users\hoa.le\Downloads\NewVyxproject2";  // Path to renamed images
            string bucketName = "xx";  // Change to your S3 bucket name
            string awsAccessKey = "xx";
            string awsSecretKey = "xxx";
            RegionEndpoint bucketRegion = RegionEndpoint.APSoutheast2;  // Change if needed
            string excelFilePath = @"C:\Users\hoa.le\Downloads\NewVyxproject\S3_VyxAssets_Mapping.xlsx"; // Path to save Excel file

            if (!Directory.Exists(localRootFolder))
            {
                Console.WriteLine("❌ Local folder not found!");
                return;
            }

            // Ensure export folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(excelFilePath));

            // AWS S3 client setup
            var s3Client = new AmazonS3Client(awsAccessKey, awsSecretKey, bucketRegion);
            var transferUtility = new TransferUtility(s3Client);

            // Clean up the bucket
            await CleanUpBucketAsync(s3Client, bucketName);

            // Scan all files in subdirectories
            var imageFiles = Directory.GetFiles(localRootFolder, "*.*", SearchOption.AllDirectories)
                                      .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                                  f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                  f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                                  f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                                      .ToList();

            Console.WriteLine($"📂 Found {imageFiles.Count} images. Uploading to S3...");

            // List to store file mappings
            List<List<string>> excelData = new List<List<string>>();

            foreach (var filePath in imageFiles)
            {
                string relativePath = Path.GetRelativePath(localRootFolder, filePath).Replace("\\", "/"); // Preserve folder structure
                string s3Key = relativePath;  // Keep the folder structure in S3
                string s3Url = $"https://{bucketName}.s3.amazonaws.com/{s3Key}";

                try
                {
                    await transferUtility.UploadAsync(filePath, bucketName, s3Key);
                    Console.WriteLine($"✅ Uploaded: {s3Key}");

                    // Extract folder levels & filename
                    var pathParts = relativePath.Split('/');
                    var row = pathParts.Take(pathParts.Length - 1).ToList(); // Folder levels
                    row.Add(Path.GetFileName(filePath)); // FileName
                    row.Add(s3Url); // S3URL
                    excelData.Add(row);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to upload {s3Key}: {ex.Message}");
                }
            }

            Console.WriteLine("🎉 Upload complete! Generating Excel...");

            CreateExcelFile(excelFilePath, excelData);
        }

        private static void CreateExcelFile(string excelFilePath, List<List<string>> excelData)
        {
            // Create Excel file
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Image Mapping");

                // Define headers
                var headers = new List<string> { "FolderLevel1", "FolderLevel2", "FolderLevel3", "FolderLevel4", "FileName", "S3URL" };
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }

                // Fill data
                for (int rowIdx = 0; rowIdx < excelData.Count; rowIdx++)
                {
                    var rowData = excelData[rowIdx];
                    for (int colIdx = 0; colIdx < rowData.Count; colIdx++)
                    {
                        worksheet.Cell(rowIdx + 2, colIdx + 1).Value = rowData[colIdx];
                    }
                }

                // Auto adjust column width
                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(excelFilePath);
            }

            Console.WriteLine($"📊 Excel file saved: {excelFilePath}");
        }

        public static void ProcessFolder(string sourcePath, string destinationPath)
        {
            // Ensure destination exists
            Directory.CreateDirectory(destinationPath);

            // Get all subfolders
            var subfolders = Directory.GetDirectories(sourcePath);
            foreach (var subfolder in subfolders)
            {
                string folderName = Path.GetFileName(subfolder);
                string newFolderPath = Path.Combine(destinationPath, folderName);

                // Recursive call for subfolders
                ProcessFolder(subfolder, newFolderPath);
            }

            // Get all image files in the current folder
            var imageFiles = Directory.GetFiles(sourcePath, "*.*")
                                      .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                                  f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                  f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                                  f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                                      .ToList();

            if (imageFiles.Count == 0)
                return;

            imageFiles.Sort(); // Ensure files are sorted for consistent naming

            int index = 1;
            foreach (var filePath in imageFiles)
            {
                string fileExtension = Path.GetExtension(filePath);
                string newFileName = $"Image_{index:D3}{fileExtension}";
                string newFilePath = Path.Combine(destinationPath, newFileName);

                try
                {
                    File.Copy(filePath, newFilePath);
                    Console.WriteLine($"✅ Moved: {Path.GetFileName(filePath)} → {newFilePath}");
                    index++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error moving {filePath}: {ex.Message}");
                }
            }
        }

        public static void GenerateImageName()
        {
            string sourceRoot = @"C:\Users\hoa.le\Downloads\Vyxproject";  // Your root folder
            string destinationRoot = @"C:\Users\hoa.le\Downloads\NewVyxproject2";  // Destination root

            if (!Directory.Exists(sourceRoot))
            {
                Console.WriteLine("❌ Source folder not found!");
                return;
            }

            Console.WriteLine("🔄 Scanning and processing folders...");
            ProcessFolder(sourceRoot, destinationRoot);
            Console.WriteLine("🎉 Image renaming & moving complete!");
        }

        public static void ProcessExcel(string filePath, string outputSqlPath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("❌ Excel file not found!");
                return;
            }

            StringBuilder sqlBuilder = new StringBuilder();

            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet("S3ResouceMapping"); // Read only the "S3ResourceMapping" sheet
                if (worksheet == null)
                {
                    Console.WriteLine("❌ Sheet 'S3ResourceMapping' not found!");
                    return;
                }

                int rowCount = worksheet.LastRowUsed().RowNumber();

                Console.WriteLine("🔄 Reading Excel file from 'S3ResourceMapping' sheet...");

                for (int row = 2; row <= rowCount; row++) // Start from row 2 (skip header)
                {
                    string productType = worksheet.Cell(row, 1).GetString();
                    string name = worksheet.Cell(row, 2).GetString();
                    string description = worksheet.Cell(row, 3).GetString();
                    string imageName = worksheet.Cell(row, 4).GetString();
                    string code = worksheet.Cell(row, 5).GetString();
                    string priceText = worksheet.Cell(row, 6).GetString();
                    string s3Url = worksheet.Cell(row, 8).GetString(); // S3Url in column 8

                    // Convert price to decimal safely
                    decimal price = decimal.TryParse(priceText, out decimal tempPrice) ? tempPrice : 0.000000m;

                    // If Code is empty, replace with ImageName
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        code = imageName;
                    }

                    // Escape single quotes to prevent SQL errors
                    name = name.Replace("'", "''");
                    description = description.Replace("'", "''");
                    code = code.Replace("'", "''");

                    // Generate SQL INSERT statement
                    string sql = $"INSERT INTO Product (ProductType, Name, Description, S3Url, Code, Price) " +
                                 $"VALUES ('{productType}', '{name}', '{description}', '{s3Url}', '{code}', {price});";

                    sqlBuilder.AppendLine(sql);
                    Console.WriteLine($"✅ Generated SQL: {sql}");
                }
            }

            // Save SQL queries to a file
            File.WriteAllText(outputSqlPath, sqlBuilder.ToString(), Encoding.UTF8);
            Console.WriteLine($"🎉 SQL file saved: {outputSqlPath}");
        }

        public static void GetStripeBalance()
        {
            // Set your secret API key
            StripeConfiguration.ApiKey = "xxx";

            try
            {
                // Create a BalanceService to fetch balance
                var service = new BalanceService();
                Balance balance = service.Get();

                // Display balance information
                Console.WriteLine("Available Balance:");
                foreach (var money in balance.Available)
                {
                    Console.WriteLine($"Amount: {money.Amount} {money.Currency}");
                }
            }
            catch (StripeException e)
            {
                Console.WriteLine($"Stripe API Error: {e.Message}");
            }
        }

        public static void ExportProductDataToTxt(string excelFilePath, string outputTxtFilePath)
        {
            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheet("Image Mapping");
                var rows = worksheet.RowsUsed().Skip(1); // Skip header row

                using (var writer = new StreamWriter(outputTxtFilePath))
                {
                    foreach (var row in rows)
                    {
                        var folderLevel1 = row.Cell(1).GetString();
                        var folderLevel2 = row.Cell(2).GetString();
                        var folderLevel3 = row.Cell(3).GetString();
                        var folderLevel4 = row.Cell(4).GetString();
                        var fileName = row.Cell(5).GetString();
                        var s3Url = row.Cell(6).GetString();

                        string productType = GetProductType(folderLevel1);
                        string priceType = GetPriceType(folderLevel2);

                        string sql = $"INSERT INTO Product (Name, Description, S3Url, ProductType, PriceType, Code, Price, IsMain) " +
                                     $"VALUES ('{fileName}', '', '{s3Url}', '{productType}', '{priceType}', '{fileName}', 0.00, false);";
                        writer.WriteLine(sql);
                    }
                }
            }

            Console.WriteLine($"📄 Product data exported to: {outputTxtFilePath}");
        }

        private static string GetProductType(string folderName)
        {
            return folderName switch
            {
                "TrouserType" => "TrouserOnly",
                "Lining" => "Lining",
                "SuitType" => "SuitType",
                "FabricOption" => "FabricOptions",
                "DesignOfSuit" => "DesignOfSuit",
                "Button" => "Button",
                _ => throw new ArgumentException("Invalid folder name for ProductType")
            };
        }

        private static string GetPriceType(string folderName)
        {
            return folderName switch
            {
                "CashmereBlend" => "SuitCahmereWool",
                "MerinoWool" => "SuitMerinoWool",
                "Linen200GSM" => "SuitLinen200GSM",
                "SuperWool150" => "SuitSuperWool150",
                "Velvette" => "SuitVelvette",
                _ => throw new ArgumentException("Invalid folder name for PriceType")
            };
        }
    }
}
