using System;
using System.Collections.Generic;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Meta
{
    /// <summary>
    /// Generates a runtime LevelData for endless mode. The returned instance
    /// is created via ScriptableObject.CreateInstance and marked
    /// IsEphemeral=true so LevelController takes the fragments-only reward
    /// path (no progression / sector / star-save side effects).
    ///
    /// v1 supports Linear y = kx + b for ChooseFunction and ChooseCoordinate.
    /// </summary>
    public class EndlessLevelGenerator
    {
        const int MaxRerolls = 32;
        const float ViewYLimit = 10f;
        const float PlaneExtent = 20f;

        public LevelData Generate(EndlessOptions opts)
        {
            int seed = opts.Seed ?? Environment.TickCount;
            var rng = new System.Random(seed);
            var band = ResolveBand(opts.Difficulty);

            for (int attempt = 0; attempt < MaxRerolls; attempt++)
            {
                if (TryBuild(opts, rng, band, seed, out var level))
                    return level;
            }

            Debug.LogWarning("[EndlessLevelGenerator] All rerolls failed quality filters; using fallback.");
            return BuildFallback(opts, seed);
        }

        // =========================================================================
        // Difficulty bands
        // =========================================================================

        struct DifficultyBand
        {
            public float[] KPool;
            public int BMin;
            public int BMax;
            public int StarCount;
            public int FragmentReward;
        }

        static DifficultyBand ResolveBand(EndlessDifficulty d) => d switch
        {
            EndlessDifficulty.Easy => new DifficultyBand
            {
                KPool = new[] { 1f, -1f, 2f, -2f },
                BMin = -3, BMax = 3,
                StarCount = 3,
                FragmentReward = 5
            },
            EndlessDifficulty.Medium => new DifficultyBand
            {
                KPool = new[] { 1f, -1f, 2f, -2f, 0.5f, -0.5f },
                BMin = -5, BMax = 5,
                StarCount = 4,
                FragmentReward = 10
            },
            EndlessDifficulty.Hard => new DifficultyBand
            {
                KPool = new[] { 1f, -1f, 2f, -2f, 3f, -3f, 0.5f, -0.5f, 1.5f, -1.5f },
                BMin = -5, BMax = 5,
                StarCount = 4,
                FragmentReward = 15
            },
            _ => ResolveBand(EndlessDifficulty.Easy)
        };

        // =========================================================================
        // Build attempt
        // =========================================================================

        bool TryBuild(EndlessOptions opts, System.Random rng, DifficultyBand band,
                      int seed, out LevelData level)
        {
            float k = band.KPool[rng.Next(band.KPool.Length)];
            int b = rng.Next(band.BMin, band.BMax + 1);

            // Fractional k → step by 2 so y stays integer-friendly.
            int xStep = Mathf.Approximately(k, Mathf.Round(k)) ? 1 : 2;

            // ChooseCoordinate splits the star set into hint stars (showing the
            // pattern, BelongsToSolution=false) plus exactly one answer star at
            // the extrapolation point (BelongsToSolution=true). Validation in
            // LevelController.ValidateChooseCoordinate matches the picked
            // option's Coordinate against unsolved solution-star coordinates,
            // so the answer-star's coord is what makes a pick "correct".
            //
            // ChooseFunction keeps the original layout where every visible
            // star is part of the solution, since validation there compares
            // the picked option's Function coefficients against the reference.
            StarConfig[] stars;
            int answerStarIndex = -1;
            if (opts.TaskType == TaskType.ChooseCoordinate)
            {
                stars = TryPlaceStars(k, b, xStep, band.StarCount + 1, rng);
                if (stars == null) { level = null; return false; }
                answerStarIndex = stars.Length - 1;
                for (int i = 0; i < stars.Length; i++)
                {
                    var s = stars[i];
                    s.BelongsToSolution = i == answerStarIndex;
                    s.IsControlPoint = i == 0;
                    stars[i] = s;
                }
            }
            else
            {
                stars = TryPlaceStars(k, b, xStep, band.StarCount, rng);
                if (stars == null) { level = null; return false; }
            }

            var (correctFunc, options) = opts.TaskType switch
            {
                TaskType.ChooseFunction => BuildChooseFunctionOptions(k, b, band, rng),
                TaskType.ChooseCoordinate => BuildChooseCoordinateOptions(
                    k, b, stars[answerStarIndex].Coordinate, rng),
                _ => (null, null)
            };

            if (correctFunc == null || options == null) { level = null; return false; }

            level = ScriptableObject.CreateInstance<LevelData>();
            level.name = $"Endless_{opts.Difficulty}_{opts.TaskType}_{seed}";
            level.LevelId = $"endless_{opts.Difficulty.ToString().ToLowerInvariant()}_" +
                            $"{opts.TaskType.ToString().ToLowerInvariant()}_{seed}";
            level.LevelIndex = 0;
            level.Type = LevelType.Normal;
            level.PlaneMin = new Vector2(-PlaneExtent, -PlaneExtent);
            level.PlaneMax = new Vector2(PlaneExtent, PlaneExtent);
            level.GridStep = 1f;
            level.Stars = stars;
            level.TaskType = opts.TaskType;
            level.ReferenceFunctions = new[] { correctFunc };
            level.AnswerOptions = options;
            level.AccuracyThreshold = 0.5f;
            level.StarRating = new StarRatingConfig
            {
                ThreeStarMaxErrors = 0,
                TwoStarMaxErrors = 1,
                OneStarMaxErrors = 3,
                TimerAffectsRating = false,
                ThreeStarMaxTime = 0f
            };
            level.MaxAttempts = 0;
            level.MaxAdjustments = 0;
            level.AllowedFunctionTypes = Array.Empty<FunctionType>();
            level.UseMemoryMode = false;
            level.MemoryDisplayDuration = 0f;
            level.GraphVisibility = default;
            level.ShowHints = false;
            level.Hints = Array.Empty<HintConfig>();
            level.FragmentReward = band.FragmentReward;
            level.IsEphemeral = true;
            return true;
        }

        // =========================================================================
        // Star placement
        // =========================================================================

        StarConfig[] TryPlaceStars(float k, int b, int xStep, int count, System.Random rng)
        {
            // Center the run of stars roughly around x=0. Random offset adds
            // variety between rolls without pushing stars off the visible grid.
            int xOffset = rng.Next(-1, 2); // -1, 0, or 1
            int xStart = -(count / 2) * xStep + xOffset;

            var stars = new StarConfig[count];
            for (int i = 0; i < count; i++)
            {
                int x = xStart + i * xStep;
                float y = k * x + b;

                // Reject star sets where any point leaves the visible band.
                if (Mathf.Abs(y) > ViewYLimit) return null;
                if (Mathf.Abs(x) > ViewYLimit) return null;

                stars[i] = new StarConfig
                {
                    StarId = $"endless_star_{i + 1}",
                    Coordinate = new Vector2(x, y),
                    InitialState = StarState.Active,
                    IsControlPoint = i == 0,
                    IsDistractor = false,
                    BelongsToSolution = true,
                    RevealAfterAction = -1
                };
            }
            return stars;
        }

        // =========================================================================
        // ChooseFunction options
        // =========================================================================

        (FunctionDefinition correct, AnswerOption[] options) BuildChooseFunctionOptions(
            float k, float b, DifficultyBand band, System.Random rng)
        {
            var correctFunc = MakeFunction(k, b);
            var taken = new HashSet<(float, float)> { (k, b) };

            var distractorRecipes = new List<(float dk, float db)>
            {
                (k, b + 1),
                (k, b - 1),
                (k + 1, b),
                (k - 1, b),
                (-k, b)
            };
            Shuffle(distractorRecipes, rng);

            var options = new List<AnswerOption>
            {
                new AnswerOption
                {
                    OptionId = "opt_correct",
                    Text = FormatLinear(k, b),
                    IsCorrect = true,
                    Function = correctFunc
                }
            };

            int distId = 1;
            foreach (var (dk, db) in distractorRecipes)
            {
                if (taken.Contains((dk, db))) continue;
                if (Mathf.Approximately(dk, 0f)) continue;
                taken.Add((dk, db));

                options.Add(new AnswerOption
                {
                    OptionId = $"opt_dist_{distId++}",
                    Text = FormatLinear(dk, db),
                    IsCorrect = false,
                    Function = MakeFunction(dk, db)
                });

                if (options.Count == 4) break;
            }

            if (options.Count != 4) return (null, null);

            Shuffle(options, rng);
            return (correctFunc, options.ToArray());
        }

        // =========================================================================
        // ChooseCoordinate options
        // =========================================================================

        (FunctionDefinition correct, AnswerOption[] options) BuildChooseCoordinateOptions(
            float k, int b, Vector2 answer, System.Random rng)
        {
            var correctFunc = MakeFunction(k, b);

            // The correct option must carry the answer star's exact Coordinate
            // — that's what LevelController.ValidateChooseCoordinate matches
            // against. Distractors must NOT collide with it (otherwise they
            // become correct too).
            float qx = answer.x;
            float qy = answer.y;

            var taken = new HashSet<(float, float)> { (qx, qy) };
            var recipes = new List<(float dx, float dy)>
            {
                (qx + 1, qy),
                (qx - 1, qy),
                (qx, qy + 1),
                (qx, qy - 1),
                (qx + 1, qy + 1),
                (qx - 1, qy - 1)
            };
            Shuffle(recipes, rng);

            var options = new List<AnswerOption>
            {
                new AnswerOption
                {
                    OptionId = "opt_correct",
                    Text = FormatPoint(qx, qy),
                    IsCorrect = true,
                    Coordinate = new Vector2(qx, qy)
                }
            };

            int distId = 1;
            foreach (var (dx, dy) in recipes)
            {
                if (taken.Contains((dx, dy))) continue;
                taken.Add((dx, dy));

                options.Add(new AnswerOption
                {
                    OptionId = $"opt_dist_{distId++}",
                    Text = FormatPoint(dx, dy),
                    IsCorrect = false,
                    Coordinate = new Vector2(dx, dy)
                });

                if (options.Count == 4) break;
            }

            if (options.Count != 4) return (null, null);

            Shuffle(options, rng);
            return (correctFunc, options.ToArray());
        }

        // =========================================================================
        // Helpers
        // =========================================================================

        static FunctionDefinition MakeFunction(float k, float b)
        {
            var f = ScriptableObject.CreateInstance<FunctionDefinition>();
            f.name = $"endless_fn_{k}_{b}";
            f.Type = FunctionType.Linear;
            f.Coefficients = new[] { k, b };
            f.DomainRange = new Vector2(-PlaneExtent, PlaneExtent);
            return f;
        }

        static string FormatLinear(float k, float b)
        {
            string kPart = Mathf.Approximately(k, 1f) ? "x"
                         : Mathf.Approximately(k, -1f) ? "-x"
                         : $"{FormatNumber(k)}x";

            if (Mathf.Approximately(b, 0f)) return $"y = {kPart}";
            return b > 0f ? $"y = {kPart} + {FormatNumber(b)}"
                          : $"y = {kPart} - {FormatNumber(-b)}";
        }

        static string FormatPoint(float x, float y) =>
            $"({FormatNumber(x)}, {FormatNumber(y)})";

        static string FormatNumber(float v)
        {
            // Use invariant culture so "0.5" doesn't render as "0,5" on
            // localized devices.
            return v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // =========================================================================
        // Fallback
        // =========================================================================

        LevelData BuildFallback(EndlessOptions opts, int seed)
        {
            // Hand-crafted minimum that always passes filters: y = x + 1,
            // 3 stars, ChooseFunction with three slope/intercept distractors.
            var rng = new System.Random(seed);
            var band = ResolveBand(EndlessDifficulty.Easy);
            var stars = TryPlaceStars(1f, 1, 1, 3, rng);
            var (correct, opts4) = BuildChooseFunctionOptions(1f, 1f, band, rng);

            var level = ScriptableObject.CreateInstance<LevelData>();
            level.name = $"Endless_Fallback_{seed}";
            level.LevelId = $"endless_fallback_{seed}";
            level.Type = LevelType.Normal;
            level.PlaneMin = new Vector2(-PlaneExtent, -PlaneExtent);
            level.PlaneMax = new Vector2(PlaneExtent, PlaneExtent);
            level.GridStep = 1f;
            level.Stars = stars;
            level.TaskType = TaskType.ChooseFunction;
            level.ReferenceFunctions = new[] { correct };
            level.AnswerOptions = opts4;
            level.AccuracyThreshold = 0.5f;
            level.StarRating = new StarRatingConfig
            {
                ThreeStarMaxErrors = 0,
                TwoStarMaxErrors = 1,
                OneStarMaxErrors = 3
            };
            level.AllowedFunctionTypes = Array.Empty<FunctionType>();
            level.Hints = Array.Empty<HintConfig>();
            level.FragmentReward = band.FragmentReward;
            level.IsEphemeral = true;
            return level;
        }
    }
}
