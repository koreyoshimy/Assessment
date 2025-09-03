using Dapper;
using Assessment.Services;   // or whatever namespace contains FreelancerService
using Assessment.Models;     // or whatever namespace contains Freelancer
using Assessment.Repository;
using Xunit;
using Moq;

namespace Application.Test.cs
{
    public class ApplicationTest
    {
        [Fact]
        public async Task Create_ShouldReturnCreatedFreelancer()
        {
            // Arrange
            var mockRepo = new Mock<FreelancerRepository>(); // assuming you have an interface
            var freelancerToCreate = new Freelancer
            {
                Username = "testuser",
                Email = "test@example.com",
                Phone = "0123456789",
                Skillsets = new List<Skillset> { new Skillset { Name = "C#" } },
                Hobbies = new List<Hobby> { new Hobby { Name = "Chess" } }
            };
            var mockService = new Mock<IFreelancerService>();
            // Mock repository to return the same freelancer
            mockService.Setup(r => r.Create(It.IsAny<Freelancer>()))
                    .ReturnsAsync((Freelancer f) => f);

            var service = new FreelancerService(mockRepo.Object);

            // Act
            var createdFreelancer = await service.Create(freelancerToCreate);

            // Assert
            Assert.NotNull(createdFreelancer);
            Assert.Equal("testuser", createdFreelancer.Username);
            Assert.Equal("test@example.com", createdFreelancer.Email);
            Assert.Single(createdFreelancer.Skillsets);
            Assert.Single(createdFreelancer.Hobbies);

            // Verify repository Create() was called exactly once
            mockService.Verify(r => r.Create(It.IsAny<Freelancer>()), Times.Once);
        }
    }
}