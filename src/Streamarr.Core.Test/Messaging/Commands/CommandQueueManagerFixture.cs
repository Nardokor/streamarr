using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Streamarr.Common.Composition;
using Streamarr.Core.Exceptions;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Messaging.Commands
{
    [TestFixture]
    public class CommandQueueManagerFixture : CoreTest<CommandQueueManager>
    {
        [SetUp]
        public void Setup()
        {
            var id = 0;
            var commands = new List<CommandModel>();

            Mocker.GetMock<ICommandRepository>()
                  .Setup(s => s.Insert(It.IsAny<CommandModel>()))
                  .Returns<CommandModel>(c =>
                  {
                      c.Id = id + 1;
                      commands.Add(c);
                      id++;

                      return c;
                  });

            Mocker.GetMock<ICommandRepository>()
                  .Setup(s => s.Get(It.IsAny<int>()))
                  .Returns<int>(c =>
                  {
                      return commands.SingleOrDefault(e => e.Id == c);
                  });
        }

        [Test]
        public void should_throw_bad_request_when_command_name_is_unknown()
        {
            Mocker.SetConstant(new KnownTypes(new List<Type> { typeof(MessagingCleanupCommand) }));

            var ex = Assert.Throws<StreamarrClientException>(() =>
                Subject.Push("NotARealCommand", null, null));

            ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Message.Should().Contain("NotARealCommand");
        }

        [Test]
        public void should_push_command_by_name_when_known()
        {
            Mocker.SetConstant(new KnownTypes(new List<Type> { typeof(MessagingCleanupCommand) }));

            var result = Subject.Push("MessagingCleanupCommand", null, null);

            result.Should().NotBeNull();
            result.Name.Should().Be("MessagingCleanup");
        }

        [Test]
        public void should_not_remove_commands_for_five_minutes_after_they_end()
        {
            var command = Subject.Push<MessagingCleanupCommand>(new MessagingCleanupCommand());

            // Start the command to mimic CommandQueue's behaviour
            command.StartedAt = DateTime.Now;
            command.Status = CommandStatus.Started;

            Subject.Start(command);
            Subject.Complete(command, "All done");
            Subject.CleanCommands();

            Subject.Get(command.Id).Should().NotBeNull();

            Mocker.GetMock<ICommandRepository>()
                  .Verify(v => v.Get(It.IsAny<int>()), Times.Never());
        }
    }
}
