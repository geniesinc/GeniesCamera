using System.IO;

namespace GUnityGLTF.Loader
{
	public interface IDataLoader2 : IDataLoader
	{
		Stream LoadStream(string relativeFilePath);
	}
}
