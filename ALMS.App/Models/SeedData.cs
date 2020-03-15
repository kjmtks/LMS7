using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ALMS.App.Models.Entities;

namespace ALMS.App.Models
{
    public class SeedData
    {
        public static void Initialize(DatabaseContext context)
        {
            var t = context.Users.AnyAsync();
            t.Wait();
            if (t.Result)
            {
                // DB has been seeded  
                return;
            }
            context.SandboxTemplates.Add(new SandboxTemplate()
            {
                Name = "ruby",
                Subject = "Ruby 2.7.0",
                Description = "install command: ruby",
                SetupCommands = @"apt-get update && apt-get install -y build-essential libffi-dev zlib1g-dev libssl-dev libreadline-dev libgdbm-dev libbison-dev libmariadbclient-dev
mkdir /tmp
cd tmp
wget --no-check-certificate https://cache.ruby-lang.org/pub/ruby/2.7/ruby-2.7.0.tar.gz
tar xzf ruby-2.7.0.tar.gz
cd /tmp/ruby-2.7.0
./configure
make
make install"
            });
            context.SandboxTemplates.Add(new SandboxTemplate()
            {
                Name = "python3",
                Subject = "Python 3.8.2",
                Description = "install commands: python3, pip3",
                SetupCommands = @"apt-get update && apt-get install -y build-essential zlib1g-dev libncurses5-dev libgdbm-dev libnss3-dev libssl-dev libreadline-dev libffi-dev libbz2-dev cmake
mkdir /tmp
cd /tmp
wget --no-check-certificate https://www.python.org/ftp/python/3.8.2/Python-3.8.2.tgz
tar xvfzp Python-3.8.2.tgz
cd /tmp/Python-3.8.2
./configure --enable-optimizations
make
make install"
            });


            var user = new User()
            {
                Account = "admin",
                Password = "admin",
                DisplayName = "Administrator",
                EmailAddress = "admin@localhost",
                IsAdmin = true,
                IsSenior = true,
            };
            user.CreateNew(context);

            for (int i = 0; i < 10; i++)
            {
                var num = string.Format("{0:000}", i+1);
                var test_user = new User()
                {
                    Account = $"test{num}",
                    Password = $"test{num}",
                    DisplayName = $"Test User {num}",
                    EmailAddress = $"test{num}@localhost"
                };
                test_user.CreateNew(context);
            }

            context.SaveChanges();
        }
    }
}
