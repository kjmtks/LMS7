using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Diagnostics;

using ALMS.App.Models;
using ALMS.App.Models.Contents;
using ALMS.App.Components.Contents;

namespace ALMS.App.Services
{

    public partial class ActivityService
    {
        public IBackgroundTaskQueueSet Queue { get; set; }
        public DatabaseService DatabaseService { get; set; }
        public ActivityService(DatabaseService databaseService, IBackgroundTaskQueueSet queue) 
        {
            DatabaseService = databaseService;
            Queue = queue;
        }

        public async Task<(bool, string)> SaveAsync(Models.Contents.Activity activity, Models.Entities.Lecture lecture, Models.Entities.User user)
        {
            try
            {
                var assign = DatabaseService.Context.LectureUsers.Include(x => x.User).Include(x => x.Lecture).ThenInclude(x => x.Owner).Where(x => x.UserId == user.Id && x.LectureId == lecture.Id).FirstOrDefault();
                if (assign == null) return (false, "The user is not assigned");
                var time = DateTime.Now;
                foreach (var f in activity.GetChildRenderFragments().Select(x => x.Item1))
                {
                    var fileInfo = new FileInfo($"{user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home/{activity.Directory}/{f.Name}");
                    if (!fileInfo.Directory.Exists)
                    {
                        fileInfo.Directory.Create();
                        Process.Start("chown", $"-R {user.Id + 1000}:{user.Id + 1000} {user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home").WaitForExit();
                    }

                    if (activity.GetFileComponents().ContainsKey(f.Name))
                    {
                        if (activity.GetFileComponents()[f.Name] is UploadActivityComponent uac)
                        {
                            if (uac.Data != null)
                            {
                                using (var w = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write))
                                {
                                    w.Write(uac.Data, 0, uac.Data.Length);
                                }
                            }
                        }
                        else
                        {
                            using (var w = new StreamWriter(fileInfo.FullName))
                            {
                                w.Write(await activity.GetFileComponents()[f.Name].GetValueAsync());
                            }
                        }
                        //Process.Start("chown", $" {user.Id + 1000}:{user.Id + 1000} {fileInfo.FullName}").WaitForExit();
                        Process.Start("chown", $"-R {user.Id + 1000}:{user.Id + 1000} {user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home").WaitForExit();
                        Console.WriteLine($"DEBUG_LOG: SAVE FILE {fileInfo.FullName}");
                    }
                }

                // Check certainly saved
                //foreach (var f in activity.GetChildRenderFragments().Select(x => x.Item1))
                //{
                //    var fileInfo = new FileInfo($"{user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home/{activity.Directory}/{f.Name}");
                //    if(!fileInfo.Exists)
                //    {
                //        return (false, "Failure to save. Please retry.");
                //    }
                //}

                assign.RepositoryPair.ClonedRepository.CommitChanges($"[Activity] Name=\"{activity.Name}\" Action=\"Save\" DateTime=\"{time.ToString("yyyy-MM-ddTHH:mm:sszzz")}\"", user.DisplayName, user.EmailAddress);
                assign.RepositoryPair.ClonedRepository.Push();

                DatabaseService.Context.ActivityActionHistories.Add(new Models.Entities.ActivityActionHistory()
                {
                    User = user,
                    Lecture = lecture,
                    ActivityName = activity.Name,
                    Directory = activity.Directory,
                    ActionType = Models.Entities.ActivityActionType.Save,
                    DateTime = time
                });
                DatabaseService.Context.SaveChanges();

                return (true, "Files were saved successfully");
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }



        public async Task RunAsync(Models.Contents.Activity activity, Models.Entities.Lecture lecture, Models.Entities.User user,
                Action<string> stdoutCallback = null,
                Action<string> stderrCallback = null,
                Action<string> cmdCallback = null,
                Action<int?, bool, string> doneCallback = null)
        {
            try
            {
                var assign = DatabaseService.Context.LectureUsers.Include(x => x.User).Include(x => x.Lecture).ThenInclude(x => x.Owner).Where(x => x.UserId == user.Id && x.LectureId == lecture.Id).FirstOrDefault();
                if (assign == null) { doneCallback(null, false, "The user is not assigned"); return; }

                var sandbox = DatabaseService.Context.LectureSandboxes.Where(x => x.Name == activity.Sandbox && x.LectureId == lecture.Id).FirstOrDefault();
                if (sandbox == null) { doneCallback(null, false, "Not found sandbox"); return; }

                var time = DateTime.Now;
                foreach (var f in activity.GetChildRenderFragments().Select(x => x.Item1))
                {
                    var fileInfo = new FileInfo($"{user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home/{activity.Directory}/{f.Name}");
                    if (!fileInfo.Directory.Exists)
                    {
                        fileInfo.Directory.Create();
                        Process.Start("chown", $"-R {user.Id + 1000}:{user.Id + 1000} {user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home").WaitForExit();
                    }

                    if (activity.GetFileComponents().ContainsKey(f.Name))
                    {
                        if (activity.GetFileComponents()[f.Name] is UploadActivityComponent uac)
                        {
                            if (uac.Data != null)
                            {
                                using (var w = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write))
                                {
                                    w.Write(uac.Data, 0, uac.Data.Length);
                                }
                            }
                        }
                        else
                        {
                            using (var w = new StreamWriter(fileInfo.FullName))
                            {
                                w.Write(await activity.GetFileComponents()[f.Name].GetValueAsync());
                            }
                        }
                        //Process.Start("chown", $" {user.Id + 1000}:{user.Id + 1000} {fileInfo.FullName}").WaitForExit();
                        Process.Start("chown", $"-R {user.Id + 1000}:{user.Id + 1000} {user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home").WaitForExit();
                        Console.WriteLine($"DEBUG_LOG: SAVE FILE BEFORE RUN {fileInfo.FullName}");
                    }
                }

                // Check certainly saved
                //foreach (var f in activity.GetChildRenderFragments().Select(x => x.Item1))
                //{
                //    var fileInfo = new FileInfo($"{user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home/{activity.Directory}/{f.Name}");
                //    if (!fileInfo.Exists)
                //    {
                //        doneCallback(null, false, "Failure to save. Please retry.");
                //        return;
                //    }
                //}
                assign.RepositoryPair.ClonedRepository.CommitChanges($"[Activity] Name=\"{activity.Name}\" Action=\"Save before Run\" DateTime=\"{time.ToString("yyyy-MM-ddTHH:mm:sszzz")}\"", user.DisplayName, user.EmailAddress);
                assign.RepositoryPair.ClonedRepository.Push();

                var command = $"cd ~/{activity.Directory}; {activity.Run}";
                Queue.QueueBackgroundWorkItem(async token =>
                {
                    await sandbox.DoOnSandboxWithCmdAsync(user, command, stdoutCallback, stderrCallback, cmdCallback, (code) =>
                    {
                        assign.RepositoryPair.ClonedRepository.CommitChanges($"[Activity] Name=\"{activity.Name}\" Action=\"Run\" DateTime=\"{time.ToString("yyyy-MM-ddTHH:mm:sszzz")}\"", user.DisplayName, user.EmailAddress);
                        assign.RepositoryPair.ClonedRepository.Push();
                        Console.WriteLine("Done");
                        DatabaseService.Context.ActivityActionHistories.Add(new Models.Entities.ActivityActionHistory()
                        {
                            User = user,
                            Lecture = lecture,
                            ActivityName = activity.Name,
                            Directory = activity.Directory,
                            ActionType = Models.Entities.ActivityActionType.SaveAndRun,
                            DateTime = time
                        });
                        DatabaseService.Context.SaveChanges();
                        doneCallback(code, true, "Run successfully");
                    }, activity.Limits);

                    assign.RepositoryPair.ClonedRepository.CommitChanges($"[Activity] Name=\"{activity.Name}\" Action=\"Save after Run\" DateTime=\"{time.ToString("yyyy-MM-ddTHH:mm:sszzz")}\"", user.DisplayName, user.EmailAddress);
                    assign.RepositoryPair.ClonedRepository.Push();
                }, user.IsTeacher(lecture));
            }
            catch (Exception e)
            {
                doneCallback(null, false, e.Message);
            }
        }



        public async Task SubmitAsync(Models.Contents.Activity activity, Models.Entities.Lecture lecture, Models.Entities.User user,
                Action<int?, bool, string> doneCallback = null)
        {
            try
            {
                var assign = DatabaseService.Context.LectureUsers.Include(x => x.User).Include(x => x.Lecture).ThenInclude(x => x.Owner).Where(x => x.UserId == user.Id && x.LectureId == lecture.Id).FirstOrDefault();
                if (assign == null) { doneCallback(null, false, "The user is not assigned"); return; }
                var time = DateTime.Now;

                foreach (var f in activity.GetChildRenderFragments().Select(x => x.Item1))
                {
                    var fileInfo = new FileInfo($"{user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home/{activity.Directory}/{f.Name}");
                    if (!fileInfo.Directory.Exists)
                    {
                        fileInfo.Directory.Create();
                        Process.Start("chown", $"-R {user.Id + 1000}:{user.Id + 1000} {user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home").WaitForExit();
                    }

                    if (activity.GetFileComponents().ContainsKey(f.Name))
                    {
                        if (activity.GetFileComponents()[f.Name] is UploadActivityComponent uac)
                        {
                            if (uac.Data != null)
                            {
                                using (var w = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write))
                                {
                                    w.Write(uac.Data, 0, uac.Data.Length);
                                }
                                uac.SetSavedFileInfo(fileInfo);
                            }
                        }
                        else
                        {
                            using (var w = new StreamWriter(fileInfo.FullName))
                            {
                                w.Write(await activity.GetFileComponents()[f.Name].GetValueAsync());
                            }
                        }
                        // Process.Start("chown", $" {user.Id + 1000}:{user.Id + 1000} {fileInfo.FullName}").WaitForExit();
                        Process.Start("chown", $"-R {user.Id + 1000}:{user.Id + 1000} {user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home").WaitForExit();
                        Console.WriteLine($"DEBUG_LOG: SAVE FILE BEFORE SUBMIT {fileInfo.FullName}");
                    }
                }

                // Check certainly saved
                //foreach (var f in activity.GetChildRenderFragments().Select(x => x.Item1))
                //{
                //    var fileInfo = new FileInfo($"{user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home/{activity.Directory}/{f.Name}");
                //    if (!fileInfo.Exists)
                //    {
                //        doneCallback(null, false, "Failure to save. Please retry.");
                //        return;
                //    }
                //}

                assign.RepositoryPair.ClonedRepository.CommitChanges($"[Activity] Name=\"{activity.Name}\" Action=\"Save before Submit\" DateTime=\"{time.ToString("yyyy-MM-ddTHH:mm:sszzz")}\"", user.DisplayName, user.EmailAddress);
                assign.RepositoryPair.ClonedRepository.Push();


                var submit_file = new FileInfo($"{user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home/{activity.Directory}/SUBMIT");
                var command = $"cd ~/{activity.Directory}; {activity.Submit}";
                Queue.QueueBackgroundWorkItem(async token =>
                {

                    if (!string.IsNullOrWhiteSpace(activity.Submit))
                    {
                        var sandbox = DatabaseService.Context.LectureSandboxes.Where(x => x.Name == activity.Sandbox && x.LectureId == lecture.Id).FirstOrDefault();
                        if (sandbox == null) { doneCallback(null, false, "Not found sandbox"); return; }

                        var sb = new System.Text.StringBuilder();
                        await sandbox.DoOnSandboxWithCmdAsync(user, command, (stdout) => { sb.AppendLine(stdout); }, null, null, (code) =>
                        {
                            using (var w = new StreamWriter(submit_file.FullName))
                            {
                                w.Write(sb.ToString());
                            }
                        });
                        if (!submit_file.Exists)
                        {
                            doneCallback(null, false, "Failure to submit. Please retry.");
                            return;
                        }

                        assign.RepositoryPair.ClonedRepository.CommitChanges($"[Activity] Name=\"{activity.Name}\" Action=\"Save Submit Summary\" DateTime=\"{time.ToString("yyyy-MM-ddTHH:mm:sszzz")}\"", user.DisplayName, user.EmailAddress);
                        assign.RepositoryPair.ClonedRepository.Push();
                    }


                    foreach (var f in activity.GetChildRenderFragments().Select(x => x.Item1))
                    {
                        var fileInfo = new FileInfo($"{lecture.DirectoryPath}/submissions/{user.Account}/{activity.Name}/{f.Name}");
                        if (!fileInfo.Directory.Exists)
                        {
                            fileInfo.Directory.Create();
                        }

                        if (activity.GetFileComponents().ContainsKey(f.Name))
                        {
                            if (activity.GetFileComponents()[f.Name] is UploadActivityComponent uac)
                            {
                                if (uac.Data != null)
                                {
                                    using (var w = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write))
                                    {
                                        w.Write(uac.Data, 0, uac.Data.Length);
                                    }
                                    uac.SetSubmittedFileInfo(fileInfo);
                                }
                            }
                            else
                            {
                                using (var w = new StreamWriter(fileInfo.FullName))
                                {
                                    w.Write(await activity.GetFileComponents()[f.Name].GetValueAsync());
                                }
                            }
                        }
                        Console.WriteLine($"DEBUG_LOG: SAVE FILE OF SUBMISSION {fileInfo.FullName}");
                    }


                    // Check certainly submit
                    //foreach (var f in activity.GetChildRenderFragments().Select(x => x.Item1))
                    //{
                    //    var fileInfo = new FileInfo($"{lecture.DirectoryPath}/submissions/{user.Account}/{activity.Name}/{f.Name}");
                    //    if (!fileInfo.Exists)
                    //    {
                    //        doneCallback(null, false, "Failure to submit. Please retry.");
                    //        return;
                    //    }
                    //}

                    if (!string.IsNullOrWhiteSpace(activity.Submit))
                    {
                        var source = new FileInfo($"{user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home/{activity.Directory}/SUBMIT");
                        var target = new FileInfo($"{lecture.DirectoryPath}/submissions/{user.Account}/{activity.Name}/SUBMIT");
                        File.Copy(source.FullName, target.FullName, true);
                        if (!target.Exists)
                        {
                            doneCallback(null, false, "Failure to submit. Please retry.");
                            return;
                        }
                    }

                    lecture.LectureSubmissionsRepositoryPair.ClonedRepository.CommitChanges($"[Activity] Name=\"{activity.Name}\" Action=\"Submit\" DateTime=\"{time.ToString("yyyy-MM-ddTHH:mm:sszzz")}\"", user.DisplayName, user.EmailAddress);
                    lecture.LectureSubmissionsRepositoryPair.ClonedRepository.Push();

                    DatabaseService.Context.ActivityActionHistories.Add(new Models.Entities.ActivityActionHistory()
                    {
                        User = user,
                        Lecture = lecture,
                        ActivityName = activity.Name,
                        Directory = activity.Directory,
                        ActionType = Models.Entities.ActivityActionType.SaveAndSubmit,
                        DateTime = time
                    });
                    DatabaseService.Context.SaveChanges();
                    doneCallback(null, true, "Files were submitted successfully");

                }, user.IsTeacher(lecture));
            }
            catch (Exception e)
            {
                doneCallback(null, false, e.Message);
            }
        }

        public async Task ValidateAsync(Models.Contents.Activity activity, Models.Entities.Lecture lecture, Models.Entities.User user,
                Action<bool?, bool, string> doneCallback = null)
        {
            try
            {
                var assign = DatabaseService.Context.LectureUsers.Include(x => x.User).Include(x => x.Lecture).ThenInclude(x => x.Owner).Where(x => x.UserId == user.Id && x.LectureId == lecture.Id).FirstOrDefault();
                if (assign == null) doneCallback(null, false, "The user is not assigned");

                var sandbox = DatabaseService.Context.LectureSandboxes.Where(x => x.Name == activity.Sandbox && x.LectureId == lecture.Id).FirstOrDefault();
                if (sandbox == null) doneCallback(null, false, "Not found sandbox");

                var time = DateTime.Now;
                foreach (var f in activity.GetChildRenderFragments().Select(x => x.Item1))
                {
                    var fileInfo = new FileInfo($"{user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home/{activity.Directory}/{f.Name}");
                    if (!fileInfo.Directory.Exists)
                    {
                        fileInfo.Directory.Create();
                        Process.Start("chown", $"-R {user.Id + 1000}:{user.Id + 1000} {user.DirectoryPath}/l{lecture.Owner.Account}/ecture_data/{lecture.Name}/home").WaitForExit();
                    }

                    if (activity.GetFileComponents().ContainsKey(f.Name))
                    {
                        if (activity.GetFileComponents()[f.Name] is UploadActivityComponent uac)
                        {
                            if (uac.Data != null)
                            {
                                using (var w = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write))
                                {
                                    w.Write(uac.Data, 0, uac.Data.Length);
                                }
                            }
                        }
                        else
                        {
                            using (var w = new StreamWriter(fileInfo.FullName))
                            {
                                w.Write(await activity.GetFileComponents()[f.Name].GetValueAsync());
                            }
                        }
                        //Process.Start("chown", $" {user.Id + 1000}:{user.Id + 1000} {fileInfo.FullName}").WaitForExit();
                        Process.Start("chown", $"-R {user.Id + 1000}:{user.Id + 1000} {user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home").WaitForExit();
                        Console.WriteLine($"DEBUG_LOG: SAVE FILE BEFORE VALIDATION {fileInfo.FullName}");
                    }
                }

                // Check certainly saved
                //foreach (var f in activity.GetChildRenderFragments().Select(x => x.Item1))
                //{
                //    var fileInfo = new FileInfo($"{user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home/{activity.Directory}/{f.Name}");
                //    if (!fileInfo.Exists)
                //    {
                //        doneCallback(null, false, "Failure to save. Please retry.");
                //        return;
                //    }
                //}
                assign.RepositoryPair.ClonedRepository.CommitChanges($"[Activity] Name=\"{activity.Name}\" Action=\"Save before Validate\" DateTime=\"{time.ToString("yyyy-MM-ddTHH:mm:sszzz")}\"", user.DisplayName, user.EmailAddress);
                assign.RepositoryPair.ClonedRepository.Push();

                Queue.QueueBackgroundWorkItem(async token =>
                {
                    var accept = await (activity.Validations.Child as ALMS.App.Models.Contents.IValidatable).ValidateAsync(async validation =>
                    {
                        if (string.IsNullOrWhiteSpace(validation.Run)) { return false; }
                        var command = $"cd ~/{activity.Directory}; {validation.Run}";
                        var stdout = new System.Text.StringBuilder();
                        var stderr = new System.Text.StringBuilder();
                        await sandbox.DoOnSandboxAsync(user.Account, command,
                            data => { stdout.AppendLine(data); }, data => { stderr.AppendLine(data); }, null, activity.Limits);

                        if (validation.Type.ToLower() == "equals")
                        {
                            return stdout.ToString().Trim() == validation.Answer.Trim();
                        }
                        return false;
                    });

                    try
                    {
                        assign.RepositoryPair.ClonedRepository.CommitChanges($"[Activity] Name=\"{activity.Name}\" Action=\"Validate\" DateTime=\"{time.ToString("yyyy-MM-ddTHH:mm:sszzz")}\"", user.DisplayName, user.EmailAddress);
                        assign.RepositoryPair.ClonedRepository.Push();

                        DatabaseService.Context.ActivityActionHistories.Add(new Models.Entities.ActivityActionHistory()
                        {
                            User = user,
                            Lecture = lecture,
                            ActivityName = activity.Name,
                            Directory = activity.Directory,
                            ActionType = accept ? Models.Entities.ActivityActionType.SaveAndValidateAccept : Models.Entities.ActivityActionType.SaveAndValidateReject,
                            DateTime = time
                        });
                        DatabaseService.Context.SaveChanges();
                        doneCallback(accept, true, "Validate successfully");
                    }
                    catch (Exception e)
                    {
                        doneCallback(null, false, e.Message);
                    }
                }, user.IsTeacher(lecture));
            }
            catch (Exception e)
            {
                doneCallback(null, false, e.Message);
            }
        }




        public async Task SetInitialValueAsync(Models.Contents.Activity activity, Models.Entities.Lecture lecture, Models.Entities.User user)
        {
            foreach (var f in activity.GetChildRenderFragments().Select(x => x.Item1))
            {
                var fileInfo = new FileInfo($"{user.DirectoryPath}/lecture_data/{lecture.Owner.Account}/{lecture.Name}/home/{activity.Directory}/{f.Name}");
                if (fileInfo.Exists && activity.GetFileComponents().ContainsKey(f.Name))
                {
                    if (activity.GetFileComponents()[f.Name] is UploadActivityComponent u)
                    {
                        u.SetSavedFileInfo(fileInfo);
                    }
                    else
                    {
                        using (var t = new StreamReader(fileInfo.FullName))
                        {
                            await activity.GetFileComponents()[f.Name].SetValueAsync(t.ReadToEnd());
                        }
                    }
                }
                else
                {
                    await activity.GetFileComponents()[f.Name].SetDefaultValueAsync();
                }

                var submittedFileInfo = new FileInfo($"{lecture.DirectoryPath}/submissions/{user.Account}/{activity.Name}/{f.Name}");
                if (submittedFileInfo.Exists)
                {
                    if (activity.GetFileComponents()[f.Name] is UploadActivityComponent u)
                    {
                        u.SetSubmittedFileInfo(fileInfo);
                    }
                }
            }
        }



    }


}
