using Moq;
using OmerkckEF.Biscom.Interfaces;
using OmerkckEF.Biscom.ToolKit;
using System.Data;
using Xunit;

namespace OmerkckEF.Bisco.Tests
{
    public class MapperTests
    {
        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void EntityMapper_MapList_ShouldMapPropertiesCorrectly()
        {
            // Arrange
            var readerMock = new Mock<IDataReader>();
            var metadataMock = new Mock<IMetadataProvider>();

            // Mocking reader data
            readerMock.SetupSequence(r => r.Read())
                      .Returns(true)
                      .Returns(false);

            readerMock.Setup(r => r.FieldCount).Returns(2);
            readerMock.Setup(r => r.GetName(0)).Returns("Id");
            readerMock.Setup(r => r.GetName(1)).Returns("Name");
            readerMock.Setup(r => r.GetOrdinal("Id")).Returns(0);
            readerMock.Setup(r => r.GetOrdinal("Name")).Returns(1);
            readerMock.Setup(r => r.GetValue(0)).Returns(1);
            readerMock.Setup(r => r.GetValue(1)).Returns("Test Name");

            // Mocking MetadataProvider to actually set values
            metadataMock.Setup(m => m.ParsePrimitive(It.IsAny<System.Reflection.PropertyInfo>(), It.IsAny<object>(), It.IsAny<object>()))
                        .Callback<System.Reflection.PropertyInfo, object, object?>((prop, entity, value) => {
                            prop.SetValue(entity, value);
                        });

            // Act
            var result = EntityMapper.MapList<TestEntity>(readerMock.Object, metadataMock.Object);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Test Name", result[0].Name);
        }
    }
}
