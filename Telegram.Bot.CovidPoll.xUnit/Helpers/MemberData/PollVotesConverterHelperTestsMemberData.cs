using System.Collections.Generic;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Helpers.Models;

namespace Telegram.Bot.CovidPoll.xUnit.Helpers.MemberData
{
    public class PollVotesConverterHelperTestsMemberData
    {
        public static IEnumerable<object[]> GetAnswersWithExpectedResults()
        {
            yield return new object[]
            {
                new PollChat()
                {
                    PollAnswers = new List<PollAnswer>()
                    {
                        new PollAnswer
                        {
                            UserId = 1,
                            Username = "username1",
                            UserFirstName = "userfirstname1",
                            VoteNumber = 100
                        }
                    },
                    NonPollAnswers = new List<NonPollAnswer>()
                    {
                        new NonPollAnswer
                        {
                            UserId = 2,
                            Username = "username2",
                            UserFirstName = "userfirstname2",
                            VoteNumber = 200
                        }
                    }
                },
                new List<PredictionsModel>()
                {
                    new PredictionsModel()
                    {
                        UserId = 1,
                        Username = "username1",
                        UserFirstName = "userfirstname1",
                        VoteNumber = 100,
                        Points = 0
                    },
                    new PredictionsModel()
                    {
                        UserId = 2,
                        Username = "username2",
                        UserFirstName = "userfirstname2",
                        VoteNumber = 200,
                        Points = 0
                    }
                },
                null
            };

            yield return new object[]
            {
                new PollChat()
                {
                    PollAnswers = new List<PollAnswer>()
                    {
                        new PollAnswer
                        {
                            UserId = 1,
                            Username = "username1",
                            UserFirstName = "userfirstname1",
                            VoteNumber = 200
                        }
                    },
                    NonPollAnswers = new List<NonPollAnswer>()
                    {
                        new NonPollAnswer
                        {
                            UserId = 2,
                            Username = "username2",
                            UserFirstName = "userfirstname2",
                            VoteNumber = 400
                        }
                    }
                },
                new List<PredictionsModel>()
                {
                    new PredictionsModel()
                    {
                        UserId = 1,
                        Username = "username1",
                        UserFirstName = "userfirstname1",
                        VoteNumber = 200,
                        Points = 9
                    },
                    new PredictionsModel()
                    {
                        UserId = 2,
                        Username = "username2",
                        UserFirstName = "userfirstname2",
                        VoteNumber = 400,
                        Points = 10
                    }
                },
                500
            };
        }

        public static IEnumerable<object[]> GetPollWithExpectedResults()
        {
            yield return new object[]
            {
                new Poll()
                {
                    ChatPolls = new List<PollChat>()
                    {
                        new PollChat()
                        {
                            ChatId = 1,
                            PollAnswers = new List<PollAnswer>()
                            {
                                new PollAnswer
                                {
                                    UserId = 1,
                                    Username = "username1",
                                    UserFirstName = "userfirstname1",
                                    VoteNumber = 100
                                }
                            },
                            NonPollAnswers = new List<NonPollAnswer>()
                            {
                                new NonPollAnswer
                                {
                                    UserId = 2,
                                    Username = "username2",
                                    UserFirstName = "userfirstname2",
                                    VoteNumber = 200
                                },
                                new NonPollAnswer
                                {
                                    UserId = 3,
                                    Username = "username3",
                                    UserFirstName = "userfirstname3",
                                    VoteNumber = 300
                                }
                            }
                        },
                        new PollChat()
                        {
                            ChatId = 2,
                            PollAnswers = new List<PollAnswer>()
                            {
                                new PollAnswer
                                {
                                    UserId = 1,
                                    Username = "username1",
                                    UserFirstName = "userfirstname1",
                                    VoteNumber = 1000
                                }
                            }
                        }
                    }
                },
                new List<UserRatio>()
                {
                    new UserRatio()
                    {
                        ChatId = 1,
                        UserId = 1,
                        Ratio = 0.5
                    },
                    new UserRatio()
                    {
                        ChatId = 1,
                        UserId = 3,
                        Ratio = 0.2
                    },
                    new UserRatio()
                    {
                        ChatId = 2,
                        UserId = 1,
                        Ratio = 0.6
                    }
                },
                546
            };

            yield return new object[]
            {
                new Poll()
                {
                    ChatPolls = new List<PollChat>()
                    {
                        new PollChat()
                        {
                            ChatId = 1,
                            PollAnswers = new List<PollAnswer>()
                            {
                                new PollAnswer
                                {
                                    UserId = 1,
                                    Username = "username1",
                                    UserFirstName = "userfirstname1",
                                    VoteNumber = 100
                                }
                            }
                        },
                        new PollChat()
                        {
                            ChatId = 2,
                            PollAnswers = new List<PollAnswer>()
                            {
                                new PollAnswer
                                {
                                    UserId = 2,
                                    Username = "username1",
                                    UserFirstName = "userfirstname1",
                                    VoteNumber = 1000
                                }
                            }
                        }
                    }
                },
                new List<UserRatio>(){},
                550
            };

            yield return new object[]
            {
                new Poll(){},
                new List<UserRatio>(){},
                null
            };
        }
    }
}
