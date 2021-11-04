﻿using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TagsCloudVisualization
{
    [TestFixture]
    public class CircularCloudLayouterTests
    {
        private CircularCloudLayouter layouter;

        [TearDown]
        public void TearDown()
        {
            var context = TestContext.CurrentContext;
            if (context.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                if (!Directory.Exists("testresults"))
                    Directory.CreateDirectory("testresults");
                var rectangles = (List<Rectangle>)typeof(CircularCloudLayouter).GetField("lastRectangles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(layouter);
                Visualizer.GetBitmapFromRectangles(rectangles.ToArray(), true).Save($"testresults/{context.Test.Name}.png", ImageFormat.Png);
            }
        }

        [Test]
        public void ShouldHaveSetCenter_OnCreation()
        {
            var expectedCenter = new Point(10, 11);
            layouter = new CircularCloudLayouter(expectedCenter);
            layouter.Center.Should().Be(expectedCenter);
        }

        [Test]
        public void ShouldReturnRectangleWithSetSize()
        {
            layouter = new CircularCloudLayouter(Point.Empty);
            var expectedSize = new Size(10, 11);

            var resulRectangle = layouter.PutNextRectangle(expectedSize);

            resulRectangle.Size.Should().Be(expectedSize);
        }

        [Test]
        public void ShouldThrow_WhenTryingToPutNegativeSizedRectangle()
        {
            var expectedCenter = Point.Empty;
            layouter = new CircularCloudLayouter(expectedCenter);
            Action action = () => layouter.PutNextRectangle(new Size(-1, -1));
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase(0, 2)]
        [TestCase(0, 2)]
        [TestCase(0, 0)]
        public void ShouldThrow_WhenTryingToPutDegenerateRectangle(int width, int height)
        {
            var expectedCenter = Point.Empty;
            layouter = new CircularCloudLayouter(expectedCenter);
            Action action = () => layouter.PutNextRectangle(new Size(width, height));
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase(0, 0)]
        [TestCase(10, 11)]
        public void FirstRectangle_ShouldHaveSameCenter(int x, int y)
        {
            var expectedCenter = new Point(x, y);
            layouter = new CircularCloudLayouter(expectedCenter);

            var resultRectangle = layouter.PutNextRectangle(new Size(10, 11));
            var resultCenter = resultRectangle.GetCenter();

            resultCenter.Should().Be(expectedCenter);
        }

        [Test]
        public void SecondRectangle_ShouldBePlacedNextToFirst()
        {
            var expectedCenter = Point.Empty;
            layouter = new CircularCloudLayouter(expectedCenter);

            var firstRectangle = layouter.PutNextRectangle(new Size(10, 11));
            var secondRectangle = layouter.PutNextRectangle(new Size(11, 10));

            secondRectangle.Should()
                .Match<Rectangle>(second =>
                firstRectangle.Bottom == second.Top
                || firstRectangle.Top == second.Bottom
                || firstRectangle.Left == second.Right
                || firstRectangle.Right == second.Left);
        }

        [Test]
        public void SecondRectangle_ShouldBePlacedAsCloseToCenterAsPossible()
        {
            var expectedCenter = Point.Empty;
            layouter = new CircularCloudLayouter(expectedCenter);
            var firstSize = new Size(10, 11);
            var secondSize = new Size(10, 11);
            var maximumExpectedDistance = (((Point)(firstSize / 2)).Length() + ((Point)(secondSize / 2)).Length()) / Math.Sqrt(2);

            layouter.PutNextRectangle(firstSize);
            var secondRectangle = layouter.PutNextRectangle(secondSize);
            var actualDistance = expectedCenter.DistanceTo(secondRectangle.GetCenter());

            actualDistance.Should().BeLessOrEqualTo(maximumExpectedDistance);
        }

        [Test]
        public void ThirdRectangle_ShouldBePlacedAsCloseToCenterAsPossible()
        {
            var expectedCenter = Point.Empty;
            layouter = new CircularCloudLayouter(expectedCenter);
            var firstSize = new Size(10, 11);
            var secondSize = new Size(10, 11);
            var maximumExpectedDistance = ((Point)(firstSize / 2)).Length() + ((Point)(secondSize / 2)).Length();

            layouter.PutNextRectangle(firstSize);
            layouter.PutNextRectangle(secondSize);
            var thirdRectangle = layouter.PutNextRectangle(secondSize);
            var actualDistance = expectedCenter.DistanceTo(thirdRectangle.GetCenter());

            actualDistance.Should().BeLessOrEqualTo(maximumExpectedDistance + 2);
        }

        [TestCase(3)]
        [TestCase(300)]
        public void EveryRectangle_ShouldNotIntersectWithEachOther(int count)
        {
            var expectedCenter = Point.Empty;
            layouter = new CircularCloudLayouter(expectedCenter);
            var size = new Size(10, 11);

            var rectangles = new List<Rectangle>(count);
            for (var i = 0; i < count; i++)
            {
                rectangles.Add(layouter.PutNextRectangle(size));
            }

            var resultRectangles = rectangles.Select((rectangle, i) => (rectangle, i));

            resultRectangles.Should()
                .OnlyContain(rectangle => resultRectangles
                    .Where(x => x.i != rectangle.i)
                    .All(x => !x.rectangle.IntersectsWith(rectangle.rectangle)),
                "every rectangle should not intersect with others");
        }

        [TestCase(20000)]
        public void PuttingRectanglesShouldBeFast(int milliseconds)
        {
            layouter = new CircularCloudLayouter(Point.Empty);
            var size = new Size(10, 11);

            Action action = () =>
              {
                  for (var i = 0; i < 1300; i++)
                  {
                      layouter.PutNextRectangle(size);
                  }
              };

            action.ExecutionTime().Should().BeLessOrEqualTo(new TimeSpan(0, 0, 0, 0, milliseconds));
        }
    }
}
