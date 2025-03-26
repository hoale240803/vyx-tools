using GenerateImageName;

namespace GenerateImageNameSpace
{
    public class MainClass
    {
        static async Task Main()
        {
            //await FunctionHelper.UploadImageToS3Async();
            //FunctionHelper.GenerateImageName();

            //string excelFilePath = @"C:\Users\YourUser\Downloads\ProductionData.xlsx"; // Update with actual path
            string excelFilePath = @"C:\Users\hoa.le\Downloads\VyxProject.xlsx";
            string outputSqlPath = @"C:\Users\hoa.le\Downloads\InsertProduct.sql";
            //string outputSqlPath = @"C:\Users\YourUser\Downloads\InsertProduct.sql"; // Output SQL file
            //FunctionHelper.ProcessExcel(excelFilePath, outputSqlPath);
            FunctionHelper.GetStripeBalance();
        }
    }
}
