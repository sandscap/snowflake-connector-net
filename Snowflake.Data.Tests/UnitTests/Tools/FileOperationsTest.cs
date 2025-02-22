/*
 * Copyright (c) 2024 Snowflake Computing Inc. All rights reserved.
 */

using System.Collections.Generic;
using Snowflake.Data.Core;
using System.IO;
using System.Runtime.InteropServices;
using Mono.Unix;
using Mono.Unix.Native;
using NUnit.Framework;
using Snowflake.Data.Core.Tools;
using static Snowflake.Data.Tests.UnitTests.Configuration.EasyLoggingConfigGenerator;
using System.Security;

namespace Snowflake.Data.Tests.Tools
{
    [TestFixture, NonParallelizable]
    public class FileOperationsTest
    {
        private static FileOperations s_fileOperations;
        private static readonly string s_workingDirectory = Path.Combine(Path.GetTempPath(), "file_operations_test_", Path.GetRandomFileName());

        [OneTimeSetUp]
        public static void BeforeAll()
        {
            if (!Directory.Exists(s_workingDirectory))
            {
                Directory.CreateDirectory(s_workingDirectory);
            }

            s_fileOperations = new FileOperations();
        }

        [OneTimeTearDown]
        public static void AfterAll()
        {
            Directory.Delete(s_workingDirectory, true);
        }

        [Test]
        public void TestReadAllTextOnWindows()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("skip test only runs on Windows");
            }

            var content = "random text";
            var filePath = CreateConfigTempFile(s_workingDirectory, content);

            // act
            var result = s_fileOperations.ReadAllText(filePath, TomlConnectionBuilder.ValidateFilePermissions);

            // assert
            Assert.AreEqual(content, result);
        }

        [Test]
        public void TestReadAllTextCheckingPermissionsUsingTomlConfigurationFileValidations(
            [ValueSource(nameof(UserAllowedFilePermissions))]
            FileAccessPermissions userAllowedFilePermissions)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("skip test on Windows");
            }

            var content = "random text";
            var filePath = CreateConfigTempFile(s_workingDirectory, content);
            var filePermissions = userAllowedFilePermissions;

        Syscall.chmod(filePath, (FilePermissions)filePermissions);

            // act
            var result = s_fileOperations.ReadAllText(filePath, TomlConnectionBuilder.ValidateFilePermissions);

            // assert
            Assert.AreEqual(content, result);
        }

        [Test]
        public void TestShouldThrowExceptionIfOtherPermissionsIsSetWhenReadConfigurationFile(
            [ValueSource(nameof(UserAllowedFilePermissions))]
            FileAccessPermissions userAllowedFilePermissions)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("skip test on Windows");
            }

            var content = "random text";
            var filePath = CreateConfigTempFile(s_workingDirectory, content);
            var filePermissions = userAllowedFilePermissions | FileAccessPermissions.OtherReadWriteExecute;

            Syscall.chmod(filePath, (FilePermissions)filePermissions);

            // act and assert
            Assert.Throws<SecurityException>(() => s_fileOperations.ReadAllText(filePath, TomlConnectionBuilder.ValidateFilePermissions),
                "Attempting to read a file with too broad permissions assigned");
        }


        public static IEnumerable<FileAccessPermissions> UserAllowedFilePermissions()
        {
            yield return FileAccessPermissions.UserRead;
            yield return FileAccessPermissions.UserRead | FileAccessPermissions.UserWrite;
        }
    }
}
