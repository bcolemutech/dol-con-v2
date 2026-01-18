namespace DolCon.Tests;

using DolCon.Views;
using FluentAssertions;

public class PaginatedListTests
{
    [Fact]
    public void Constructor_WithEmptyList_HasZeroItems()
    {
        var list = new PaginatedList<int>([], 10);

        list.TotalItems.Should().Be(0);
        list.TotalPages.Should().Be(0);
        list.CurrentPage.Should().Be(0);
        list.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithItems_CalculatesTotalPages()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.TotalItems.Should().Be(25);
        list.TotalPages.Should().Be(3);
    }

    [Fact]
    public void Constructor_WithExactPageBoundary_CalculatesTotalPages()
    {
        var items = Enumerable.Range(1, 20).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.TotalItems.Should().Be(20);
        list.TotalPages.Should().Be(2);
    }

    [Fact]
    public void CurrentPageItems_ReturnsFirstPage()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.CurrentPageItems.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
    }

    [Fact]
    public void CurrentPageItems_ReturnsPartialLastPage()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.NextPage();
        list.NextPage();

        list.CurrentPageItems.Should().BeEquivalentTo(new[] { 21, 22, 23, 24, 25 });
    }

    [Fact]
    public void MoveDown_WithinPage_IncrementsSelectedIndex()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.MoveDown();

        list.SelectedIndex.Should().Be(1);
        list.CurrentPage.Should().Be(0);
    }

    [Fact]
    public void MoveDown_AtPageEnd_AdvancesToNextPage()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        for (var i = 0; i < 10; i++)
        {
            list.MoveDown();
        }

        list.SelectedIndex.Should().Be(10);
        list.CurrentPage.Should().Be(1);
        list.CurrentPageSelectedIndex.Should().Be(0);
    }

    [Fact]
    public void MoveDown_AtLastItem_StaysAtLastItem()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        for (var i = 0; i < 30; i++)
        {
            list.MoveDown();
        }

        list.SelectedIndex.Should().Be(24);
        list.CurrentPage.Should().Be(2);
    }

    [Fact]
    public void MoveUp_WithinPage_DecrementsSelectedIndex()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.MoveDown();
        list.MoveDown();
        list.MoveUp();

        list.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void MoveUp_AtPageStart_GoesToPreviousPage()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.NextPage();
        list.MoveUp();

        list.SelectedIndex.Should().Be(9);
        list.CurrentPage.Should().Be(0);
        list.CurrentPageSelectedIndex.Should().Be(9);
    }

    [Fact]
    public void MoveUp_AtFirstItem_StaysAtFirstItem()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.MoveUp();

        list.SelectedIndex.Should().Be(0);
        list.CurrentPage.Should().Be(0);
    }

    [Fact]
    public void NextPage_AdvancesPage()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.NextPage();

        list.CurrentPage.Should().Be(1);
        list.SelectedIndex.Should().Be(10);
    }

    [Fact]
    public void NextPage_AtLastPage_StaysOnLastPage()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.NextPage();
        list.NextPage();
        list.NextPage();
        list.NextPage();

        list.CurrentPage.Should().Be(2);
    }

    [Fact]
    public void PreviousPage_GoesBackOnePage()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.NextPage();
        list.PreviousPage();

        list.CurrentPage.Should().Be(0);
    }

    [Fact]
    public void PreviousPage_AtFirstPage_StaysOnFirstPage()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.PreviousPage();

        list.CurrentPage.Should().Be(0);
    }

    [Fact]
    public void Reset_SetsIndexToZeroAndFirstPage()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.NextPage();
        list.MoveDown();
        list.MoveDown();
        list.Reset();

        list.SelectedIndex.Should().Be(0);
        list.CurrentPage.Should().Be(0);
    }

    [Fact]
    public void GetSelected_ReturnsCurrentlySelectedItem()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.MoveDown();
        list.MoveDown();

        list.GetSelected().Should().Be(3);
    }

    [Fact]
    public void GetSelected_WithEmptyList_ReturnsDefault()
    {
        var list = new PaginatedList<int>([], 10);

        list.GetSelected().Should().Be(0);
    }

    [Fact]
    public void CurrentPageSelectedIndex_ReturnsIndexWithinPage()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.NextPage();
        list.MoveDown();
        list.MoveDown();

        list.CurrentPageSelectedIndex.Should().Be(2);
    }

    [Fact]
    public void PageInfo_ReturnsFormattedString()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.PageInfo.Should().Be("Page 1/3 (Items 1-10 of 25)");

        list.NextPage();

        list.PageInfo.Should().Be("Page 2/3 (Items 11-20 of 25)");

        list.NextPage();

        list.PageInfo.Should().Be("Page 3/3 (Items 21-25 of 25)");
    }

    [Fact]
    public void PageInfo_WithEmptyList_ReturnsEmptyIndicator()
    {
        var list = new PaginatedList<int>([], 10);

        list.PageInfo.Should().Be("No items");
    }

    [Fact]
    public void UpdateItems_RefreshesListAndResets()
    {
        var items = Enumerable.Range(1, 25).ToList();
        var list = new PaginatedList<int>(items, 10);

        list.NextPage();
        list.MoveDown();

        var newItems = Enumerable.Range(100, 5).ToList();
        list.UpdateItems(newItems);

        list.TotalItems.Should().Be(5);
        list.TotalPages.Should().Be(1);
        list.CurrentPage.Should().Be(0);
        list.SelectedIndex.Should().Be(0);
        list.CurrentPageItems.Should().BeEquivalentTo(new[] { 100, 101, 102, 103, 104 });
    }
}
