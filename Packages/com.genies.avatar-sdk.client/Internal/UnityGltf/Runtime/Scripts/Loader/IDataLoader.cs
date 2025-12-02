using System.IO;
using System.Threading.Tasks;

namespace GUnityGLTF.Loader
{
	public interface IDataLoader
	{
		Task<Stream> LoadStreamAsync(string relativeFilePath);
	}
}
