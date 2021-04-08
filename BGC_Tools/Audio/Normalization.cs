using System;

namespace BGC.Audio
{
    /// <summary>
    /// A collection of some common procedures related to level scaling of Audio
    /// </summary>
    public static class Normalization
    {
        private const double TARGET_RMS = 1.0 / 32.0;
        private const double RMS_TO_PEAK = 2.8;

        public enum Scheme
        {
            RMS = 0,
            Peak,
            RMSAssigned,
            MAX
        }

        public const double dbSafetyLimit = 90.0;

        #region Stereo Normalizations

        public static void NormalizeStereo_RMS(
            double levelFactorL,
            double levelFactorR,
            float[] samples,
            float[] destination = null)
        {
            bool inplace = (destination == null);

            if (!inplace && destination.Length < samples.Length)
            {
                Debug.LogError($"Destination length ({destination.Length}) shorter than Source length ({samples.Length})");
                return;
            }

            double[] sampleSquaredSum = new double[2];
            int sampleCount = samples.Length / 2;

            for (int i = 0; i < sampleCount; i++)
            {
                sampleSquaredSum[0] += samples[2 * i] * samples[2 * i];
                sampleSquaredSum[1] += samples[2 * i + 1] * samples[2 * i + 1];
            }

            sampleSquaredSum[0] = Math.Sqrt(sampleSquaredSum[0] / sampleCount);
            sampleSquaredSum[1] = Math.Sqrt(sampleSquaredSum[1] / sampleCount);

            double maxRMS = Math.Max(sampleSquaredSum[0], sampleSquaredSum[1]);

            float scalingFactorL = (float)(levelFactorL / maxRMS);
            float scalingFactorR = (float)(levelFactorR / maxRMS);

            //Protect against some NaN Poisoning
            if (float.IsNaN(scalingFactorL) || float.IsInfinity(scalingFactorL))
            {
                scalingFactorL = 1f;
            }

            if (float.IsNaN(scalingFactorR) || float.IsInfinity(scalingFactorR))
            {
                scalingFactorR = 1f;
            }

            if (inplace)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    samples[2 * i] *= scalingFactorL;
                    samples[2 * i + 1] *= scalingFactorR;
                }
            }
            else
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    destination[2 * i] = samples[2 * i] * scalingFactorL;
                    destination[2 * i + 1] = samples[2 * i + 1] * scalingFactorR;
                }
            }
        }

        public static void NormalizeStereo_TargetRMS(
            double levelFactorL,
            double levelFactorR,
            double effectiveRMS,
            float[] samples,
            float[] destination = null)
        {
            bool inplace = (destination == null);

            if (!inplace && destination.Length < samples.Length)
            {
                Debug.LogError($"Destination length ({destination.Length}) shorter than Source length ({samples.Length})");
                return;
            }

            int sampleCount = samples.Length / 2;

            float scalingFactorL = (float)(levelFactorL / effectiveRMS);
            float scalingFactorR = (float)(levelFactorR / effectiveRMS);

            if (inplace)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    samples[2 * i] *= scalingFactorL;
                    samples[2 * i + 1] *= scalingFactorR;
                }
            }
            else
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    destination[2 * i] = samples[2 * i] * scalingFactorL;
                    destination[2 * i + 1] = samples[2 * i + 1] * scalingFactorR;
                }
            }
        }

        /// <summary>
        /// Peak Equivalence Level-Scaling
        /// </summary>
        public static void NormalizeStereo_Peak(
            double levelFactorL,
            double levelFactorR,
            float[] samples,
            float[] destination = null)
        {
            bool inplace = (destination == null);

            if (!inplace && destination.Length < samples.Length)
            {
                Debug.LogError($"Destination length ({destination.Length}) shorter than Source length ({samples.Length})");
                return;
            }

            double maxPeak = 0.0;
            int sampleCount = samples.Length / 2;

            for (int i = 0; i < sampleCount; i++)
            {
                maxPeak = Math.Max(samples[2 * i], maxPeak);
                maxPeak = Math.Max(samples[2 * i + 1], maxPeak);
            }

            float scalingFactorL = (float)(levelFactorL * RMS_TO_PEAK / maxPeak);
            float scalingFactorR = (float)(levelFactorR * RMS_TO_PEAK / maxPeak);

            if (float.IsNaN(scalingFactorL) || float.IsInfinity(scalingFactorL))
            {
                scalingFactorL = 1f;
            }

            if (float.IsNaN(scalingFactorR) || float.IsInfinity(scalingFactorR))
            {
                scalingFactorR = 1f;
            }

            if (inplace)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    samples[2 * i] *= scalingFactorL;
                    samples[2 * i + 1] *= scalingFactorR;
                }
            }
            else
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    destination[2 * i] = samples[2 * i] * scalingFactorL;
                    destination[2 * i + 1] = samples[2 * i + 1] * scalingFactorR;
                }
            }
        }

        #endregion Stereo Normalizations
        #region Mono Normalizations

        public static float[] NormalizeMono_RMS(
            double levelFactorL,
            double levelFactorR,
            float[] monoInput,
            float[] stereoOutput = null,
            int inputOffset = 0,
            int outputOffset = 0,
            int sampleCount = int.MaxValue)
        {
            if (sampleCount == int.MaxValue)
            {
                //Set sampleCount if the argument wasn't provided
                if (stereoOutput == null)
                {
                    //Determined by the input if we're creating the output
                    sampleCount = monoInput.Length - inputOffset;
                }
                else
                {
                    //Determined by the smaller of the two if we are not
                    sampleCount = Math.Min(
                        monoInput.Length - inputOffset,
                        (stereoOutput.Length - 2 * outputOffset) / 2);

                }
            }
            else if (monoInput.Length < monoInput.Length - inputOffset)
            {
                //Except out if it was provided but was unusable
                Debug.LogError($"Input length ({monoInput.Length}) shorter than required length ({monoInput.Length - inputOffset})");
                return stereoOutput;
            }

            if (stereoOutput == null)
            {
                //Create stereoOutput if the argument wasn't provided
                stereoOutput = new float[2 * (sampleCount + outputOffset)];
            }
            else if (stereoOutput.Length < 2 * (sampleCount + outputOffset))
            {
                //Except out if it was provided but was unusable
                Debug.LogError($"Output length ({stereoOutput.Length}) shorter than required length ({2 * (sampleCount + outputOffset)})");
                return stereoOutput;
            }

            double sampleSquaredSum = 0.0;

            for (int i = 0; i < sampleCount; i++)
            {
                sampleSquaredSum += monoInput[i + inputOffset] * monoInput[i + inputOffset];
            }

            double maxRMS = Math.Sqrt(sampleSquaredSum / sampleCount);

            float scalingFactorL = (float)(levelFactorL / maxRMS);
            float scalingFactorR = (float)(levelFactorR / maxRMS);

            //Protect against some NaN Poisoning
            if (float.IsNaN(scalingFactorL) || float.IsInfinity(scalingFactorL))
            {
                scalingFactorL = 1f;
            }

            if (float.IsNaN(scalingFactorR) || float.IsInfinity(scalingFactorR))
            {
                scalingFactorR = 1f;
            }

            for (int i = 0; i < sampleCount; i++)
            {
                stereoOutput[2 * (i + outputOffset)] = monoInput[i + inputOffset] * scalingFactorL;
                stereoOutput[2 * (i + outputOffset) + 1] = monoInput[i + inputOffset] * scalingFactorR;
            }

            return stereoOutput;
        }

        public static float[] NormalizeMono_TargetRMS(
            double levelFactorL,
            double levelFactorR,
            double effectiveRMS,
            float[] monoInput,
            float[] stereoOutput = null,
            int inputOffset = 0,
            int outputOffset = 0,
            int sampleCount = int.MaxValue)
        {
            if (sampleCount == int.MaxValue)
            {
                //Set sampleCount if the argument wasn't provided
                if (stereoOutput == null)
                {
                    //Determined by the input if we're creating the output
                    sampleCount = monoInput.Length - inputOffset;
                }
                else
                {
                    //Determined by the smaller of the two if we are not
                    sampleCount = Math.Min(
                        monoInput.Length - inputOffset,
                        (stereoOutput.Length - 2 * outputOffset) / 2);

                }
            }
            else if (monoInput.Length < monoInput.Length - inputOffset)
            {
                //Except out if it was provided but was unusable
                Debug.LogError($"Input length ({monoInput.Length}) shorter than required length ({monoInput.Length - inputOffset})");
                return stereoOutput;
            }

            if (stereoOutput == null)
            {
                //Create stereoOutput if the argument wasn't provided
                stereoOutput = new float[2 * (sampleCount + outputOffset)];
            }
            else if (stereoOutput.Length < 2 * (sampleCount + outputOffset))
            {
                //Except out if it was provided but was unusable
                Debug.LogError($"Output length ({stereoOutput.Length}) shorter than required length ({2 * (sampleCount + outputOffset)})");
                return stereoOutput;
            }

            float scalingFactorL = (float)(levelFactorL / effectiveRMS);
            float scalingFactorR = (float)(levelFactorR / effectiveRMS);

            for (int i = 0; i < sampleCount; i++)
            {
                stereoOutput[2 * (i + outputOffset)] = monoInput[i + inputOffset] * scalingFactorL;
                stereoOutput[2 * (i + outputOffset) + 1] = monoInput[i + inputOffset] * scalingFactorR;
            }

            return stereoOutput;
        }

        /// <summary>
        /// Peak Equivalence Level-Scaling
        /// </summary>
        public static float[] NormalizeMono_Peak(
            double levelFactorL,
            double levelFactorR,
            float[] monoInput,
            float[] stereoOutput = null,
            int inputOffset = 0,
            int outputOffset = 0,
            int sampleCount = int.MaxValue)
        {
            if (sampleCount == int.MaxValue)
            {
                //Set sampleCount if the argument wasn't provided
                if (stereoOutput == null)
                {
                    //Determined by the input if we're creating the output
                    sampleCount = monoInput.Length - inputOffset;
                }
                else
                {
                    //Determined by the smaller of the two if we are not
                    sampleCount = Math.Min(
                        monoInput.Length - inputOffset,
                        (stereoOutput.Length - 2 * outputOffset) / 2);

                }
            }
            else if (monoInput.Length < monoInput.Length - inputOffset)
            {
                //Except out if it was provided but was unusable
                Debug.LogError($"Input length ({monoInput.Length}) shorter than required length ({monoInput.Length - inputOffset})");
                return stereoOutput;
            }

            if (stereoOutput == null)
            {
                //Create stereoOutput if the argument wasn't provided
                stereoOutput = new float[2 * (sampleCount + outputOffset)];
            }
            else if (stereoOutput.Length < 2 * (sampleCount + outputOffset))
            {
                //Except out if it was provided but was unusable
                Debug.LogError($"Output length ({stereoOutput.Length}) shorter than required length ({2 * (sampleCount + outputOffset)})");
                return stereoOutput;
            }

            double maxPeak = 0.0;
            for (int i = 0; i < sampleCount; i++)
            {
                maxPeak = Math.Max(monoInput[i + inputOffset], maxPeak);
            }

            float scalingFactorL = (float)(levelFactorL * RMS_TO_PEAK / maxPeak);
            float scalingFactorR = (float)(levelFactorR * RMS_TO_PEAK / maxPeak);

            if (float.IsNaN(scalingFactorL) || float.IsInfinity(scalingFactorL))
            {
                scalingFactorL = 1f;
            }

            if (float.IsNaN(scalingFactorR) || float.IsInfinity(scalingFactorR))
            {
                scalingFactorR = 1f;
            }

            for (int i = 0; i < sampleCount; i++)
            {
                stereoOutput[2 * (i + outputOffset)] = monoInput[i + inputOffset] * scalingFactorL;
                stereoOutput[2 * (i + outputOffset) + 1] = monoInput[i + inputOffset] * scalingFactorR;
            }

            return stereoOutput;
        }

        #endregion Mono Normalizations

        public static void NormalizeRMSMono(float[] samples)
        {
            double squaredSum = 0.0;
            int sampleCount = samples.Length;

            for (int i = 0; i < sampleCount; i++)
            {
                squaredSum += samples[i] * samples[i];
            }

            squaredSum = Math.Sqrt(squaredSum / sampleCount);

            float scalingFactor = (float)(1.0 / squaredSum);

            //Protect against some NaN Poisoning
            if (float.IsNaN(scalingFactor) || float.IsInfinity(scalingFactor))
            {
                scalingFactor = 1f;
            }

            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] *= scalingFactor;
            }
        }

        public static void NormalizeRMSStereo(float[] samples)
        {
            double squaredSumL = 0.0;
            double squaredSumR = 0.0;
            int sampleCount = samples.Length / 2;

            for (int i = 0; i < sampleCount; i += 2)
            {
                squaredSumL += samples[i] * samples[i];
                squaredSumR += samples[i + 1] * samples[i + 1];
            }

            squaredSumL = Math.Sqrt(squaredSumL / sampleCount);
            squaredSumR = Math.Sqrt(squaredSumR / sampleCount);

            double squaredSum = Math.Max(squaredSumL, squaredSumR);

            float scalingFactor = (float)(1.0 / squaredSum);

            //Protect against some NaN Poisoning
            if (float.IsNaN(scalingFactor) || float.IsInfinity(scalingFactor))
            {
                scalingFactor = 1f;
                Debug.LogError("NaN Scaling Factor");
            }

            for (int i = 0; i < 2 * sampleCount; i++)
            {
                samples[i] *= scalingFactor;
            }
        }
    }
}
