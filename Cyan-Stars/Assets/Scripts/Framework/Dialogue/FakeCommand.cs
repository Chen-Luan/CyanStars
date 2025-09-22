using System.Threading;
using System.Threading.Tasks;
using MunNovel;
using MunNovel.Command;

namespace CyanStars.Framework.Dialogue
{
    public class FakeCommand : ICommand
    {
        public ValueTask ExecuteAsync(IExecutionContext ctx, CancellationToken cancellationToken = default)
        {
            var a = ctx.GetRequiredService<IFakeService>();

            a.TestFunc1();

            return default;
        }
    }
}
