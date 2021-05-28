// Dummy implementation for PCL and Unity targets
using System;
using System.IO;
using System.Text;

namespace MoonSharp.Interpreter.Platforms
{
	/// <summary>
	/// Class providing the IPlatformAccessor interface for standard full-feaured implementations.
	/// </summary>
	public class StandardPlatformAccessor : PlatformAccessorBase
	{
		public override void DefaultPrint(string content)
		{
			throw new NotImplementedException();
		}

		public override CoreModules FilterSupportedCoreModules(CoreModules module)
		{
			throw new NotImplementedException();
		}

		public override string GetEnvironmentVariable(string envvarname)
		{
			throw new NotImplementedException();
		}

		public override string GetPlatformNamePrefix()
		{
			throw new NotImplementedException();
		}

		public override Stream IO_GetStandardStream(StandardFileType type)
		{
			throw new NotImplementedException();
		}

		public override Stream IO_OpenFile(Script script, string filename, Encoding encoding, string mode)
		{
			throw new NotImplementedException();
		}

		public override string IO_OS_GetTempFilename()
		{
			throw new NotImplementedException();
		}

		public override int OS_Execute(string cmdline)
		{
			throw new NotImplementedException();
		}

		public override void OS_ExitFast(int exitCode)
		{
			throw new NotImplementedException();
		}

		public override void OS_FileDelete(string file)
		{
			throw new NotImplementedException();
		}

		public override bool OS_FileExists(string file)
		{
			throw new NotImplementedException();
		}

		public override void OS_FileMove(string src, string dst)
		{
			throw new NotImplementedException();
		}
	}
}
