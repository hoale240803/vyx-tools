using GenerateImageName;

namespace GenerateImageNameSpace
{
    public class MainClass
    {
        static async Task Main()
        {

            // 1. Generate image name
            //FunctionHelper.GenerateImageName();
            // 2. Upload image to S3
            await FunctionHelper.UploadImageToS3Async();

            string excelFilePath = @"C:\Users\hoa.le\Downloads\NewVyxproject\S3_VyxAssets_Mapping.xlsx"; // Path to save Excel file
            string txtOutputFile = @"C:\Users\hoa.le\Downloads\NewVyxproject\S3_VyxAssets_Mapping_Mariadb.txt"; // Path to save Excel file

            //string outputSqlPath = @"C:\Users\hoa.le\Downloads\InsertProduct.sql";
            //string outputSqlPath = @"C:\Users\YourUser\Downloads\InsertProduct.sql"; // Output SQL file
            //FunctionHelper.ProcessExcel(excelFilePath, outputSqlPath);
            //FunctionHelper.GetStripeBalance();

            FunctionHelper.ExportProductDataToTxt(excelFilePath, txtOutputFile);
        }
    }
}
