namespace DolCon.Tests;

using FluentAssertions;
using Models.BaseTypes;
using Newtonsoft.Json;

public class JsonConversionTests
{
    [Fact]
    public void CanConvertListWithMultipleTypesToListOfProvinces()
    {
        var json = @"
[
    0,
    {""i"":1,""state"":0,""center"":0,""burg"":0,""name"":""test1"",""formName"":"""",""fullName"":"""",""color"":""#61ddb9"",""coa"":{""t1"":"""",""ordinaries"":[],""charges"":[],""shield"":""""}},
    {""i"":2,""state"":0,""center"":0,""burg"":0,""name"":""test2"",""formName"":"""",""fullName"":"""",""color"":""#61ddb9"",""coa"":{""t1"":"""",""ordinaries"":[],""charges"":[],""shield"":""""}}
]
";
        var converter = new JsonConverter[] { new ProvincesConverter() };
        List<Province>? provinces = JsonConvert.DeserializeObject<List<Province>>(json, converter);

        provinces.Should().NotBeNull();

        provinces.Should().HaveCount(3);
        provinces[0].Should().NotBeNull();
        provinces[0].i.Should().Be(0);
        provinces[0].name.Should().BeNullOrEmpty();
    }
}
