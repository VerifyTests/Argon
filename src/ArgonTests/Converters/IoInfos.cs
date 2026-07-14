// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

#nullable enable
public class IoInfos : TestFixtureBase
{

    static DriveInfo driveInfo = DriveInfo.GetDrives()[0];
    static FileInfo fileInfo = new($"one{Path.DirectorySeparatorChar}two.txt");
    static DirectoryInfo directoryInfo = new(Path.Combine($"one{Path.DirectorySeparatorChar}two"));

    [Fact]
    public void Test_DirectoryInfo()
    {
        var serialized = JsonConvert.SerializeObject(directoryInfo);
        Assert.Equal("\"one/two\"", serialized);
        var result = JsonConvert.DeserializeObject<DirectoryInfo>(serialized);
        Assert.Equal(directoryInfo.FullName, result.FullName);
    }

    [Fact]
    public void Test_FileInfo()
    {
        var serialized = JsonConvert.SerializeObject(fileInfo);
        Assert.Equal("\"one/two.txt\"", serialized);
        var result = JsonConvert.DeserializeObject<FileInfo>(serialized);
        Assert.Equal(fileInfo.FullName, result.FullName);
    }

    [Fact]
    public void Test_DriveInfo()
    {
        var serialized = JsonConvert.SerializeObject(driveInfo);
        Assert.Equal($"\"{driveInfo.Name.Replace('\\', '/')}\"", serialized);
        var result = JsonConvert.DeserializeObject<DriveInfo>(serialized);
        Assert.Equal(driveInfo.Name, result.Name);
    }

    [Fact]
    public void Nested()
    {
        var target = new Target
        {
            DirectoryInfo = directoryInfo,
            DriveInfo = driveInfo,
            FileInfo = fileInfo
        };
        var result = JsonConvert.SerializeObject(target);
        Assert.Equal($$"""{"DirectoryInfo":"one/two","FileInfo":"one/two.txt","DriveInfo":"{{driveInfo.Name.Replace('\\', '/')}}"}""", result);
        var deserialize = JsonConvert.DeserializeObject<Target>(result);

        Assert.Equal(target.DirectoryInfo.FullName, deserialize.DirectoryInfo!.FullName);
        Assert.Equal(target.DirectoryInfo.ToString(), deserialize.DirectoryInfo!.ToString());
        Assert.Equal(target.DriveInfo.Name, deserialize.DriveInfo!.Name);
        Assert.Equal(target.FileInfo.FullName, deserialize.FileInfo!.FullName);
        Assert.Equal(target.FileInfo.ToString(), deserialize.FileInfo!.ToString());
    }

    [Fact]
    public void NestedNull()
    {
        var result = JsonConvert.SerializeObject(new Target());
        Assert.Equal("""{"DirectoryInfo":null,"FileInfo":null,"DriveInfo":null}""", result);
        JsonConvert.DeserializeObject<Target>(result);
    }

    [Fact]
    public void FileInfoThrowsForNonStringTokenInsteadOfDroppingToNull() =>
        // before the fix a non-string, non-null token silently deserialized to null
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<FileInfo>("123"));

    [Fact]
    public void FileInfoStillMapsNullTokenToNull()
    {
        var result = JsonConvert.DeserializeObject<FileInfoHolder>("""{"File":null}""");
        Assert.Null(result.File);
    }

    public class Target
    {
        public DirectoryInfo? DirectoryInfo { get; set; }
        public FileInfo? FileInfo { get; set; }
        public DriveInfo? DriveInfo { get; set; }
    }

    public class FileInfoHolder
    {
        public FileInfo? File { get; set; }
    }
}