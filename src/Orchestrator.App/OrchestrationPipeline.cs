using System.Text;
using System.Threading.Tasks;
using Orchestrator.App.Agents;

namespace Orchestrator.App;

internal sealed class OrchestrationPipeline
{
    private readonly IRoleAgent _dev;
    private readonly IRoleAgent _test;
    private readonly IRoleAgent _review;
    private readonly IRoleAgent _release;

    public OrchestrationPipeline(IRoleAgent dev, IRoleAgent test, IRoleAgent review, IRoleAgent release)
    {
        _dev = dev;
        _test = test;
        _review = review;
        _release = release;
    }

    public async Task<PipelineResult> RunAsync(WorkContext ctx)
    {
        var sb = new StringBuilder();

        var dev = await _dev.RunAsync(ctx);
        sb.AppendLine(dev.Notes);
        if (!dev.Success) return PipelineResult.Fail(sb.ToString());

        var test = await _test.RunAsync(ctx);
        sb.AppendLine(test.Notes);
        if (!test.Success) return PipelineResult.Fail(sb.ToString());

        var review = await _review.RunAsync(ctx);
        sb.AppendLine(review.Notes);
        if (!review.Success) return PipelineResult.Fail(sb.ToString());

        var release = await _release.RunAsync(ctx);
        sb.AppendLine(release.Notes);
        if (!release.Success) return PipelineResult.Fail(sb.ToString());

        var prTitle = $"Feature: {ctx.WorkItem.Title}";
        var prBody = $"Work item #{ctx.WorkItem.Number}\n\nPipeline notes\n{sb}";

        return PipelineResult.Ok(sb.ToString(), prTitle, prBody);
    }
}
