using SevenBySeven.Modules.Collection.Domain;

namespace SevenBySeven.Tests.Collection;

public class ConditionGradesTests
{
    [Theory]
    [InlineData(ConditionGrade.Mint, "M")]
    [InlineData(ConditionGrade.NearMint, "NM")]
    [InlineData(ConditionGrade.VeryGoodPlus, "VG+")]
    [InlineData(ConditionGrade.GoodPlus, "G+")]
    [InlineData(ConditionGrade.Poor, "P")]
    public void Grades_are_written_the_way_dealers_write_them(ConditionGrade grade, string expected) =>
        Assert.Equal(expected, grade.Abbreviation());

    [Fact]
    public void A_grade_is_offered_with_both_its_name_and_its_abbreviation() =>
        Assert.Equal("Very Good Plus (VG+)", ConditionGrade.VeryGoodPlus.Label());

    [Fact]
    public void Grading_is_offered_best_first()
    {
        Assert.Equal(ConditionGrade.Mint, ConditionGrades.BestFirst[0]);
        Assert.Equal(ConditionGrade.Poor, ConditionGrades.BestFirst[^1]);
        Assert.Equal(Enum.GetValues<ConditionGrade>().Length, ConditionGrades.BestFirst.Count);
    }

    [Fact]
    public void Every_grade_can_be_written_down()
    {
        foreach (var grade in Enum.GetValues<ConditionGrade>())
        {
            Assert.NotEqual(grade.ToString(), grade.Abbreviation());
            Assert.NotEmpty(grade.Describe());
        }
    }
}
