using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorInputFile;
using ALMS.App.Models.Contents;

namespace ALMS.App.Services
{
    public interface IFileUploadService
    {
        Task<byte[]> UploadAsync(IFileListEntry file, Func<long, long, int, Task> callback = null);
    }

    public class FileUploadService : IFileUploadService
    {

        public async Task<byte[]> UploadAsync(IFileListEntry file, Func<long, long, int, Task> callback = null)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                var seek = 0; var count = 0;
                byte[] buffer = new byte[1024];
                while(seek < file.Size)
                {
                    count = await file.Data.ReadAsync(buffer, 0, buffer.Length);
                    await ms.WriteAsync(buffer, 0, count);
                    if(callback!=null)
                    {
                        await callback(file.Size, seek, count);
                    }
                    seek += count;
                }
                return ms.ToArray();
            }
        }
    }
}
