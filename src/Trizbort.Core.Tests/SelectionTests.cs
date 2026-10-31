using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class SelectionTests
{
  private Project _project;

  [SetUp]
  public void SetUp()
  {
    _project = new Project();
  }

  [TearDown]
  public void TearDown()
  {
    _project.Dispose();
  }

  private Room NewRoom()
  {
    return new Room(_project);
  }

  [Test]
  public void Add_MakesElementPrimary()
  {
    var selection = new Selection();
    var a = NewRoom();
    var b = NewRoom();

    selection.Add(a).ShouldBeTrue();
    selection.Add(b).ShouldBeTrue();

    selection.Primary.ShouldBeSameAs(b);
    selection.Count.ShouldBe(2);
  }

  [Test]
  public void Add_Existing_MovesToEnd()
  {
    var selection = new Selection();
    var a = NewRoom();
    var b = NewRoom();
    selection.Add(a);
    selection.Add(b);

    selection.Add(a).ShouldBeTrue();

    selection.Items.ShouldBe(new Element[] { b, a });
    selection.Primary.ShouldBeSameAs(a);
  }

  [Test]
  public void Remove_Primary_FallsBackToPreviousItem()
  {
    var selection = new Selection();
    var a = NewRoom();
    var b = NewRoom();
    var c = NewRoom();
    selection.Set(new Element[] { a, b, c });

    selection.Remove(c).ShouldBeTrue();

    selection.Primary.ShouldBeSameAs(b);
  }

  [Test]
  public void Toggle_AddsThenRemoves()
  {
    var selection = new Selection();
    var a = NewRoom();

    selection.Toggle(a).ShouldBeTrue();
    selection.Contains(a).ShouldBeTrue();
    selection.Toggle(a).ShouldBeTrue();
    selection.Contains(a).ShouldBeFalse();
    selection.Primary.ShouldBeNull();
  }

  [Test]
  public void Click_Plain_ReplacesSelection()
  {
    var selection = new Selection();
    var a = NewRoom();
    var b = NewRoom();
    selection.Add(a);

    selection.Click(b, false, false).ShouldBeTrue();

    selection.Items.ShouldBe(new Element[] { b });
  }

  [Test]
  public void Click_Add_AppendsWithoutClearing()
  {
    var selection = new Selection();
    var a = NewRoom();
    var b = NewRoom();
    selection.Add(a);

    selection.Click(b, false, true).ShouldBeTrue();

    selection.Items.ShouldBe(new Element[] { a, b });
    selection.Primary.ShouldBeSameAs(b);
  }

  [Test]
  public void Click_Toggle_RemovesWhenAlreadySelected()
  {
    var selection = new Selection();
    var a = NewRoom();
    var b = NewRoom();
    selection.Set(new Element[] { a, b });

    selection.Click(b, true, false).ShouldBeTrue();

    selection.Items.ShouldBe(new Element[] { a });
    selection.Primary.ShouldBeSameAs(a);
  }

  [Test]
  public void Clear_OnEmpty_ReturnsFalseAndDoesNotRaise()
  {
    var selection = new Selection();
    var raised = 0;
    selection.Changed += (_, _) => raised++;

    selection.Clear().ShouldBeFalse();

    raised.ShouldBe(0);
  }

  [Test]
  public void Changed_RaisedOncePerCall()
  {
    var selection = new Selection();
    var raised = 0;
    selection.Changed += (_, _) => raised++;

    selection.Set(new Element[] { NewRoom(), NewRoom(), NewRoom() });
    raised.ShouldBe(1);

    selection.Clear();
    raised.ShouldBe(2);
  }

  [Test]
  public void Rooms_Filters_KeepsOrder()
  {
    var selection = new Selection();
    var a = NewRoom();
    var connection = new Connection(_project);
    var b = NewRoom();
    selection.Set(new Element[] { a, connection, b });

    selection.Rooms.ToList().ShouldBe(new[] { a, b });
    selection.Connections.ToList().ShouldBe(new[] { connection });
  }
}
