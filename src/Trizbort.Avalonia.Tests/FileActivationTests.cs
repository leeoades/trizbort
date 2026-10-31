using NUnit.Framework;
using Shouldly;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[Category("Unit")]
public class FileActivationTests
{
    private sealed class FakeSource : IFileActivationSource
    {
        public event Action<string>? FileActivated;

        public void Raise(string path) => FileActivated?.Invoke(path);
    }

    [Test]
    public async Task Activation_AfterReady_OpensPath()
    {
        var opened = new List<string>();
        var handler = new FileActivationHandler(p =>
        {
            opened.Add(p);
            return Task.CompletedTask;
        });
        var source = new FakeSource();
        handler.Attach(source);
        await handler.MarkReady();

        source.Raise("/maps/a.trizbort");

        opened.ShouldBe(new[] { "/maps/a.trizbort" });
    }

    [Test]
    public async Task Activation_BeforeReady_IsQueuedUntilReady()
    {
        var opened = new List<string>();
        var handler = new FileActivationHandler(p =>
        {
            opened.Add(p);
            return Task.CompletedTask;
        });
        var source = new FakeSource();
        handler.Attach(source);

        source.Raise("/maps/a.trizbort");
        source.Raise("/maps/b.trizbort");
        opened.ShouldBeEmpty();
        handler.PendingCount.ShouldBe(2);

        await handler.MarkReady();

        opened.ShouldBe(new[] { "/maps/b.trizbort" });
        handler.PendingCount.ShouldBe(0);
    }

    [Test]
    public async Task Activation_OpenFailure_DoesNotThrow()
    {
        var handler = new FileActivationHandler(_ => throw new InvalidOperationException("boom"));
        await handler.MarkReady();
        Should.NotThrow(() => handler.OnFileActivated("/maps/a.trizbort"));
    }

    [Test]
    public void BlankPath_IsIgnored()
    {
        var handler = new FileActivationHandler(_ => Task.CompletedTask);
        handler.OnFileActivated(" ");
        handler.PendingCount.ShouldBe(0);
    }
}
