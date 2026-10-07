using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.OleDb;

namespace Project.Core.Files
{
    public static class FileExtentions
    {
        public static async Task<IReadOnlyList<string>> PostFileAsync(IFormFile file, string baseDir)
        {
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = Path.GetFileNameWithoutExtension(file.FileName).ToLower().Replace(" ", "_").Replace("-", "_") + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ms") + fileExtension;
            var filePath = Path.Combine(baseDir);
            var fileDocument = Path.Combine(filePath, fileName);
            var fileSize = file.Length;
            var mimeType = file.ContentType;

            using var stream = new FileStream(fileDocument, FileMode.Create);
            await file.CopyToAsync(stream);

            return new List<string>
            {
                fileName,
                filePath,
                mimeType,
                fileExtension,
                fileSize.ToString(),
                fileDocument
            };
        }

        public static async Task<IReadOnlyList<string>> PutFileAsync(IFormFile file, string oldFileName, string baseDir)
        {
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = Path.GetFileNameWithoutExtension(file.FileName).ToLower().Replace(" ", "_").Replace("-", "_") + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ms") + fileExtension;
            var filePath = Path.Combine(baseDir);
            var fileDocument = Path.Combine(filePath, fileName);
            var fileSize = file.Length;
            var mimeType = file.ContentType;

            using var stream = new FileStream(fileDocument, FileMode.Create);
            await file.CopyToAsync(stream);

            await DeleteFileAsync(filePath, oldFileName);

            return new List<string>
            {
                fileName,
                filePath,
                mimeType,
                fileExtension,
                fileSize.ToString(),
                fileDocument
            };
        }

        public static async Task<IReadOnlyList<string>> PostFileAsync(IFormFile file, string fileName, string baseDir)
        {
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            var fileExtension = Path.GetExtension(file.FileName);
            var filePath = Path.Combine(baseDir);
            var fileDocument = Path.Combine(filePath, fileName);
            var fileSize = file.Length;
            var mimeType = file.ContentType;

            using var stream = new FileStream(fileDocument, FileMode.Create);
            await file.CopyToAsync(stream);

            return new List<string>
            {
                fileName,
                filePath,
                mimeType,
                fileExtension,
                fileSize.ToString(),
                fileDocument
            };
        }

        public static async Task<IReadOnlyList<string>> PutFileAsync(IFormFile file, string fileName, string oldFilePath, string baseDir)
        {
            var fileExtension = Path.GetExtension(file.FileName);
            var filePath = Path.Combine(baseDir);
            var fileDocument = Path.Combine(filePath, fileName);
            var fileSize = file.Length;
            var mimeType = file.ContentType;

            using var stream = new FileStream(fileDocument, FileMode.Create);
            await file.CopyToAsync(stream);

            await DeleteFileAsync(oldFilePath, fileName);

            return new List<string>
            {
                fileName,
                filePath,
                mimeType,
                fileExtension,
                fileSize.ToString(),
                fileDocument
            };
        }

        public static async Task DeleteFileAsync(string filePath, string fileName)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                if (File.Exists(Path.Combine(filePath, fileName)))
                {
                    var directory = new DirectoryInfo(filePath);
                    if (directory.GetFiles().Length == 1)
                    {
                        Directory.Delete(filePath, true);
                    }
                    else
                    {
                        using FileStream stream = new FileStream(Path.Combine(filePath, fileName), FileMode.Truncate, FileAccess.Write, FileShare.Delete, 4096, true);
                        await stream.FlushAsync();
                        File.Delete(Path.Combine(filePath, fileName));
                    }
                }
            }
        }

        public static DataTable ImportExcelOleDb(string filePath, string fileExtension)
        {
            DataTable result = new DataTable();
            try
            {
                string conString = string.Empty;

                switch (fileExtension)
                {
                    case ".xls": //Excel 97-03.
                        conString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";Extended Properties='Excel 8.0;HDR=YES'";
                        break;
                    case ".xlsx": //Excel 07 and above.
                        conString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";Extended Properties='Excel 8.0;HDR=YES'";
                        break;
                }

                conString = string.Format(conString, filePath);

                using (OleDbConnection connExcel = new OleDbConnection(conString))
                {
                    using (OleDbCommand cmdExcel = new OleDbCommand())
                    {
                        using (OleDbDataAdapter odaExcel = new OleDbDataAdapter())
                        {
                            cmdExcel.Connection = connExcel;

                            //Get the name of First Sheet.
                            connExcel.Open();
                            DataTable dtExcelSchema;
                            dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                            string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                            connExcel.Close();

                            //Read Data from First Sheet.
                            connExcel.Open();
                            // Worksheet names are identifiers and cannot be bound as parameters.
                            // Escape the closing delimiter so workbook metadata cannot alter the query.
                            var escapedSheetName = sheetName.Replace("]", "]]", StringComparison.Ordinal);
                            cmdExcel.CommandText = $"SELECT * FROM [{escapedSheetName}]";
                            odaExcel.SelectCommand = cmdExcel;
                            odaExcel.Fill(result);
                            connExcel.Close();
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                ex.ToString();
                return result;
            }
        }
    }
}
