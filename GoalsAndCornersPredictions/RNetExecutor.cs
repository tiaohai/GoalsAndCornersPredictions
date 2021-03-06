﻿using RDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoalsAndCornersPredictions
{
    public class RNETExecutor
    {
        private static readonly log4net.ILog log
          = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public RNETExecutor(String workingDirectory)
        {

        }

        public static bool Execute(String workingDirectory)
        {
            var envPath = Environment.GetEnvironmentVariable("PATH");
            var rBinPath = @"C:\Program Files\R\R-3.0.3\bin\x64";
            Environment.SetEnvironmentVariable("PATH", envPath + Path.PathSeparator + rBinPath);

            string inputPath = workingDirectory + Path.DirectorySeparatorChar + "input.txt";
            string winHPath = workingDirectory + Path.DirectorySeparatorChar + "winH.csv";
            string winAPath = workingDirectory + Path.DirectorySeparatorChar + "winA.csv";
            string likelyProbPath = workingDirectory + Path.DirectorySeparatorChar + "likelyProb.csv";
            string likelyScorePath = workingDirectory + Path.DirectorySeparatorChar + "likelyScore.csv";

            try
            {

                using (REngine engine = REngine.CreateInstance(System.Guid.NewGuid().ToString()))
                {
                    // From v1.5, REngine requires explicit initialization.
                    // You can set some parameters.
                    engine.Initialize();
                    CharacterVector ccv1 = engine.CreateCharacterVector(new List<string>() { inputPath });
                    CharacterVector ccv2 = engine.CreateCharacterVector(new List<string>() { winHPath });
                    CharacterVector ccv3 = engine.CreateCharacterVector(new List<string>() { winAPath });
                    CharacterVector ccv4 = engine.CreateCharacterVector(new List<string>() { likelyProbPath });
                    CharacterVector ccv5 = engine.CreateCharacterVector(new List<string>() { likelyScorePath });

                    engine.SetSymbol("inputFile", ccv1);
                    engine.SetSymbol("winHFile", ccv2);
                    engine.SetSymbol("winAFile", ccv3);
                    engine.SetSymbol("likelyProbFile", ccv4);
                    engine.SetSymbol("likelyScoreFile", ccv5);
                    engine.Evaluate("str(inputFile)");
                    engine.Evaluate("str(winHFile)");
                    engine.Evaluate("str(winAFile)");
                    engine.Evaluate("str(likelyProbFile)");
                    engine.Evaluate("str(likelyScoreFile)");

                    engine.Evaluate("data <- read.csv(inputFile, header=TRUE, sep = ',')");

                    engine.Evaluate("library(plyr)");

                    // goals scored by Team
                    engine.Evaluate("HomeGoals <- aggregate(data$HomeGoals, by=list(data$HomeTeam), sum)");
                    engine.Evaluate("AwayGoals <- aggregate(data$AwayGoals, by=list(data$AwayTeam), sum)");
                    engine.Evaluate("ScoreTotal <- join(HomeGoals, AwayGoals, by=\"Group.1\")");
                    engine.Evaluate("names(ScoreTotal)<-c(\"Team\",\"Home\",\"Away\")");
                    engine.Evaluate("ScoreTotal <- data.frame(Team=ScoreTotal$Team, Goals=rowSums(cbind(ScoreTotal$Home,ScoreTotal$Away), na.rm=T))");

                    engine.Evaluate("print(ScoreTotal)");
                    // number of games played by Team
                    engine.Evaluate("numGamesH <- aggregate(data$HomeTeam, by=list(data$HomeTeam), length)");
                    engine.Evaluate("numGamesA <- aggregate(data$AwayTeam, by=list(data$AwayTeam), length)");
                    engine.Evaluate("numGames <- join(numGamesH, numGamesA, by=\"Group.1\")");
                    engine.Evaluate("names(numGames)<-c(\"Team\",\"Home\",\"Away\")");
                    engine.Evaluate("numGames <- data.frame(Team=numGames$Team, numGames=rowSums(cbind(numGames$Home,numGames$Away), na.rm=T))");

                    // average number of goals scored per game
                    // not equal to sum(ScoreTotal$Goals)/nrow(data) because dropped some teams in the join
                    engine.Evaluate("meanScore <- (sum(data$HomeGoals)+sum(data$AwayGoals))/(nrow(data)*2)");

                    // adjust for how many games each team have played
                    engine.Evaluate("ScoreTotal <- cbind(ScoreTotal, attackstrength=ScoreTotal$Goals/(numGames$numGames*meanScore))");

                    // goals conceded by Team
                    engine.Evaluate("HomeGoals <- aggregate(data$AwayGoals, by=list(data$HomeTeam), sum)");
                    engine.Evaluate("AwayGoals <- aggregate(data$HomeGoals, by=list(data$AwayTeam), sum)");
                    engine.Evaluate("ConcedeTotal <- join(HomeGoals, AwayGoals, by=\"Group.1\")");
                    engine.Evaluate("names(ConcedeTotal)<-c(\"Team\",\"Home\",\"Away\")");
                    engine.Evaluate("ConcedeTotal <- data.frame(Team=ConcedeTotal$Team, Goals=rowSums(cbind(ConcedeTotal$Home,ConcedeTotal$Away), na.rm=T))");

                    // avergae conceded is same as average scored!
                    engine.Evaluate("ConcedeTotal <- cbind(ConcedeTotal, defenceweakness=ConcedeTotal$Goals/(numGames$numGames*meanScore))");

                    // average number of goals scored at home
                    engine.Evaluate("avGoalsH <- mean(data$HomeGoals)");

                    // average number of goals scored away
                    engine.Evaluate("avGoalsA <- mean(data$AwayGoals)");

                    engine.Evaluate("team.names <- unique(ScoreTotal$Team)");

                    engine.Evaluate("GoalsH <- GoalsA <- data.frame(Teams=team.names, row.names=team.names)");
                    engine.Evaluate("likelyScore <- likelyProb <- data.frame(Teams=team.names, row.names=team.names)");
                    engine.Evaluate("winH <- winA <- data.frame(Teams=team.names, row.names=team.names)");

                    engine.Evaluate("goals <- 0:5");
                    engine.Evaluate(
                    "for (HomeTeam in team.names){" + Environment.NewLine +
                      "cat (HomeTeam, \"\n\")" + Environment.NewLine +
                      "for (AwayTeam in team.names){" + Environment.NewLine +

                         // expected number of home goals                    
                        "GoalsH[HomeTeam, AwayTeam] <- avGoalsH*ScoreTotal$attackstrength[ScoreTotal$Team==HomeTeam]*ConcedeTotal$defenceweakness[ConcedeTotal$Team==AwayTeam]" + Environment.NewLine +
                        "GoalsA[HomeTeam, AwayTeam] <- avGoalsA*ScoreTotal$attackstrength[ScoreTotal$Team==AwayTeam]*ConcedeTotal$defenceweakness[ConcedeTotal$Team==HomeTeam]" + Environment.NewLine +

                        "probsH <- dpois(goals, GoalsH[HomeTeam, AwayTeam])" + Environment.NewLine +
                        "probsA <- dpois(goals, GoalsA[HomeTeam, AwayTeam])" + Environment.NewLine +
                        "likelyScore[HomeTeam, AwayTeam] <- paste(goals[probsH==max(probsH)],goals[probsA==max(probsA)])" + Environment.NewLine +
                        "likelyProb[HomeTeam, AwayTeam] <- max(probsH)*max(probsA)" + Environment.NewLine +

                        "winH[HomeTeam, AwayTeam] <- sum(sapply(goals, function(x) (1-ppois(x, lambda=GoalsH[HomeTeam, AwayTeam]))*ppois(x, lambda=GoalsA[HomeTeam, AwayTeam])))/length(goals)" + Environment.NewLine +
                        "winA[HomeTeam, AwayTeam] <- sum(sapply(goals, function(x) (1-ppois(x, lambda=GoalsA[HomeTeam, AwayTeam]))*ppois(x, lambda=GoalsH[HomeTeam, AwayTeam])))/length(goals)" + Environment.NewLine +
                        //"#draw[HomeTeam, AwayTeam] <- sum(sapply(goals, function(x) dpois(x, lambda=GoalsA[HomeTeam, AwayTeam])*dpois(x, lambda=GoalsH[HomeTeam, AwayTeam])))/length(goals)" + Environment.NewLine +
                        "}" + Environment.NewLine + "}");

                    engine.Evaluate("write.csv2(winH, winHFile, row.names=FALSE, sep=\";\",quote=FALSE)");
                    engine.Evaluate("write.csv2(winA, winAFile, row.names=FALSE, sep=\";\",quote=FALSE)");

                    engine.Evaluate("write.csv2(likelyProb, likelyProbFile, row.names=FALSE, sep=\";\",quote=FALSE)");
                    engine.Evaluate("write.csv2(likelyScore, likelyScoreFile, row.names=FALSE, sep=\";\",quote=FALSE)");
                }

                log.Debug("Running process in directory: " + workingDirectory);

                return true;
            }
            catch (Exception ce)
            {
                log.Error("REngine thru Exception " + ce);
            }

            return false;
        }
    };
}
