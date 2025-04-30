/// <summary>
        /// Set the flange brace info for the connections
        /// </summary>
        /// <param name="connections">The connections to set the flange brace info for.</param>
        /// <param name="leftBay">The bay to the left of the purlin/girt connections</param>
        /// <param name="leftBayType">The right bay type.</param>
        /// <param name="frameRun">The frame run to retrieve flange bracing from.</param>
        /// <param name="rightBay">The bay to the right of the purlin/girt connections</param>
        /// <param name="rightBayType">The right bay type.</param>
        private int SetFlangeBraceInfo(List<TeklaPurlinGirtConnection> connections, TeklaBay leftBay, PlaneTypeEnum leftBayType, FrameRun frameRun, TeklaBay rightBay, PlaneTypeEnum rightBayType, IEnumerable<Assembly> assemblies)
        {
            int fbSetNumber = -1;

            GetPurlinGirtConnectionBay(leftBay, leftBayType, rightBay, rightBayType, out CoordinateSystem csBay, out PlaneTypeEnum bayType, out double bayDepth);

            var partName = bayType == PlaneTypeEnum.Roof ? "purlin" : "girt";

            if (frameRun is null)
            {
                if (connections.Count > 0)
                {
                    var ar = new ArrayList();
                    foreach (var conn in connections.Where(c => c.FlangeBraceSize == FlangeBrace.FlangeBraceSizeEnum.Unknown))
                        ar.AddRange(GetTeklaMembers(conn));
                    ReportWarning($"Unable to determine flange bracing for {partName}s: did not find frame run data for connection location.", null, ar.ToArray());
                }
            }
            else
            {
                var t1 = GetFlangeBracing(csBay, bayType, frameRun);
                fbSetNumber = t1.Item1;
                var flangeBracing = t1.Item2;

                Vector worldZ = new Vector(0, 0, 100);
                GeometricPlane fromPlane = null;
                if (bayType == PlaneTypeEnum.Roof)
                {
                    // The purlins are listed from the ridge line to the eave, but the flange braces are from the eave to the ridge;
                    // Both are along the horizontal and not the plane of the roof.
                    Vector bayZ = csBay.AxisX.Cross(csBay.AxisY).GetNormal();
                    fromPlane = new GeometricPlane(csBay.Origin - bayZ * bayDepth, csBay.AxisX, worldZ);
                }
                else
                {
                    fromPlane = new GeometricPlane(csBay.Origin, worldZ);
                }

                InitializeFlangeBraces(connections, flangeBracing, fromPlane);

                var connectionIndex = 0;
                if (flangeBracing != null)
                {
                    foreach (var flangeBrace in flangeBracing.Where(fb => fb.ObjectToBraceTo == FlangeBrace.ObjectToBraceToEnum.Masonry).OrderBy(fb => fb.Distance))
                    {
                        if (bayType == PlaneTypeEnum.Roof)
                        {
                            ReportConnectionWarning(connections[connectionIndex], "Fixed type Flange Braces are not supported in the roof. Flange Brace has not been provided.");
                        }
                        else if (bayType == PlaneTypeEnum.Sidewall || bayType == PlaneTypeEnum.Endwall)
                        {
                            var fbPlugin = InsertFixedFlangeBrace(assemblies, flangeBrace);
                        }
                    }

                    foreach (var flangeBrace in flangeBracing.Where(fb => fb.ObjectToBraceTo == FlangeBrace.ObjectToBraceToEnum.PurlinGirt).OrderBy(fb => fb.Distance))
                    {
                        var distance = NciDistance.Inch2mm(flangeBrace.Distance);

                        if (connectionIndex < connections.Count)
                        {
                            var distanceOffset = double.MaxValue;
                            do
                            {
                                var position = connections[connectionIndex].RightPosition ?? connections[connectionIndex].LeftPosition;
                                var connectionDistance = Distance.PointToPlane(position, fromPlane);
                                distanceOffset = distance - connectionDistance;

                            } while (((Math.Round(Math.Abs(distanceOffset), 3) > NciDistance.Inch2mm(TeklaProject.FlangeBraceTolerance) && distanceOffset > 0)) &&
                                      (++connectionIndex < connections.Count));

                            //If a connection already exists at the current index > ND-3685 and ND-2423
                            if (connections[connectionIndex].FlangeBraceSize != FlangeBrace.FlangeBraceSizeEnum.None)
                            {
                                var left = connections[connectionIndex]?.LeftPurlinGirt;
                                var right = connections[connectionIndex]?.RightPurlinGirt;
                                var point = right?.Beam?.StartPoint ?? left?.Beam?.StartPoint;

                                if (point?.Z == NciDistance.Inch2mm(flangeBrace.Distance))
                                    continue;
                            }
                            //Fix: ND-1914
                            if ((distanceOffset < 0 || connectionIndex == connections.Count) &&
                                (Math.Round(Math.Abs(distanceOffset), 3) > NciDistance.Inch2mm(TeklaProject.FlangeBraceTolerance)) && connectionIndex != 0 &&
                                connections[connectionIndex - 1].FlangeBraceSize == FlangeBrace.FlangeBraceSizeEnum.None)
                            {
                                connectionIndex--;
                            }
                            if (connectionIndex < connections.Count - 1)
                            {
                                var nextPosition = connections[connectionIndex + 1].RightPosition ?? connections[connectionIndex + 1].LeftPosition;
                                if (nextPosition != null)
                                {
                                    var nextConnectionDistance = Distance.PointToPlane(nextPosition, fromPlane);
                                    var nextDistanceOffset = distance - nextConnectionDistance;

                                    if (Math.Round(Math.Abs(nextDistanceOffset), 3) <= NciDistance.Inch2mm(TeklaProject.FlangeBraceTolerance) && connections[connectionIndex + 1].FlangeBraceSize == FlangeBrace.FlangeBraceSizeEnum.None)
                                    {
                                        //Next connection is also valid
                                        if (nextDistanceOffset < distanceOffset) connectionIndex++;
                                    }
                                }

                            }

                            if (connectionIndex < connections.Count)
                            {
                                bool isParallel = false;
                                if (bayType != PlaneTypeEnum.Roof)
                                {
                                    // Determine if the girt and column flange are parallel > ND-3685 and ND-2423
                                    isParallel = IsParallelGirtWithColumn(connections[connectionIndex], assemblies, leftBayType);
                                }
                                else isParallel = true;

                                if ((Math.Round(Math.Abs(distanceOffset), 3) <= NciDistance.Inch2mm(TeklaProject.FlangeBraceTolerance)) && isParallel)
                                {
                                    //The distance is satisfied by a particular purlin / girt so set it
                                    connections[connectionIndex].SetFlangeBrace(flangeBrace);
                                }
                                else
                                {
                                    #region ND - 2423 : Add support for flange brace tolerances similar to XDS
                                    bool isAboveGirt = false;
                                    bool isBelowGirt = false;
                                    ModelObject Column = null;
                                    TeklaBay sideWallOrEndWallOrRoof = DetermineRequiredTeklaBay(leftBay, leftBayType, rightBay, rightBayType, assemblies, out Column).Item1;

                                    List<TeklaPurlinGirt> wallOrRoofGirtsOrPurlin = sideWallOrEndWallOrRoof.PurlinGirts.OrderBy(girt => girt.Beam.StartPoint.Z).ToList();
                                    List<TeklaPurlinGirt> requiredGirts = GetZeeConnectGirts(wallOrRoofGirtsOrPurlin, connections);

                                    List<(GeometricPlane, Beam)> girtAndPlanes = new List<(GeometricPlane, Beam)>();

                                    foreach (var girt in requiredGirts)
                                    {
                                        var newGirt = girt.Beam;
                                        Beam refreshedGirt = TeklaProject.Model.SelectModelObject(newGirt.Identifier) as Beam;
                                        refreshedGirt.Select();
                                        var plane = new GeometricPlane(refreshedGirt.StartPoint, refreshedGirt.GetCoordinateSystem().AxisY.GetNormal());
                                        girtAndPlanes.Add((plane, refreshedGirt));
                                    }

                                    var requiredGirtFirst = requiredGirts.FirstOrDefault()?.Beam;
                                    Beam refreshedRequiredGirtFirst = TeklaProject.Model.SelectModelObject(requiredGirtFirst.Identifier) as Beam;
                                    refreshedRequiredGirtFirst?.Select();

                                    var flangeBracePlane = new GeometricPlane(new Point(csBay.Origin + NciDistance.Inch2mm(flangeBrace.Distance) * refreshedRequiredGirtFirst?.GetCoordinateSystem().GetAxisZ().GetNormal()), refreshedRequiredGirtFirst?.GetCoordinateSystem().GetAxisZ().GetNormal());

                                    (GeometricPlane aboveGirtPlane, Beam nearestAboveGirt) = girtAndPlanes.Where(x => x.Item1.Origin.Z > flangeBracePlane.Origin.Z).OrderBy(x => x.Item1.Origin.Z).FirstOrDefault();
                                    if (nearestAboveGirt == null)
                                    {
                                        ReportWarning("No girt / purlin above flange brace location", null);
                                    }
                                    else
                                    {
                                        foreach (var connection in connections)
                                        {
                                            bool isLeftGirtMatch = IsMatchingGirt(connection?.LeftPurlinGirt?.Beam, nearestAboveGirt);
                                            bool isRightGirtMatch = IsMatchingGirt(connection?.RightPurlinGirt?.Beam, nearestAboveGirt);

                                            if ((isLeftGirtMatch || isRightGirtMatch) && IsParallelGirtWithColumn(connection, assemblies, leftBayType))
                                            {
                                                connection.SetFlangeBrace(flangeBrace);
                                                isAboveGirt = true;
                                            }
                                        }
                                    }

                                    (GeometricPlane belowGirtPlane, Beam nearestBelowGirt) = girtAndPlanes.Where(x => x.Item1.Origin.Z < flangeBracePlane.Origin.Z).OrderBy(x => x.Item1.Origin.Z).LastOrDefault();
                                    if (nearestBelowGirt == null)
                                    {
                                        ReportWarning("No girt / purlin below flange brace location", null);
                                    }
                                    else
                                    {
                                        foreach (var connection in connections)
                                        {
                                            bool isLeftGirtMatch = IsMatchingGirt(connection?.LeftPurlinGirt?.Beam, nearestBelowGirt);
                                            bool isRightGirtMatch = IsMatchingGirt(connection?.RightPurlinGirt?.Beam, nearestBelowGirt);

                                            if ((isLeftGirtMatch || isRightGirtMatch) && IsParallelGirtWithColumn(connection, assemblies, leftBayType))
                                            {
                                                connection.SetFlangeBrace(flangeBrace);
                                                isAboveGirt = true;
                                            }
                                        }
                                    }
                                    if (isBelowGirt || isAboveGirt) 
                                    {
                                        continue;
                                    }
                                    #endregion

                                    // Set the connection that is immediately before it but only if it hasn't already been set
                                    if (connections[connectionIndex].FlangeBraceOption == FlangeBrace.FlangeBracePurlinGirtConnectionEnum.Unknown && connections[connectionIndex].FlangeBraceSide == FlangeBrace.FlangeBraceSideEnum.Unknown)
                                    {
                                        connections[connectionIndex].SetFlangeBrace(flangeBrace);
                                        ReportConnectionWarning(connections[connectionIndex], $"Flange Brace position ({NciDistance.mm2Inch(distance):F2}) is beyond tolerance ({(TeklaProject.FlangeBraceTolerance):F2}\") so setting the previous {partName}.");
                                    }
                                    if (distanceOffset < 0)
                                    {
                                        //this condition is where the input point is behind the connection position but 2"(FlangeBraceTolerance) grater than the previous connection position
                                        connections[connectionIndex].SetFlangeBrace(flangeBrace);
                                    }
                                }
                            }
                        }
                        if (connectionIndex >= connections.Count)
                        {
                            ReportWarning($"Flange Brace position ({NciDistance.mm2Inch(distance):F2}) is beyond the available {partName}s.", null);
                        }

                        ++connectionIndex;
                    }
                }
            }
            return fbSetNumber;
        }

        #region Support Methods for ND - 2423
        /// <summary>
        /// This method returns match the girt
        /// </summary>
        /// <param name="original"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        bool IsMatchingGirt(Beam original, Beam target)
        {
            Beam refreshed = original == null ? null : TeklaProject.Model.SelectModelObject(original.Identifier) as Beam;
            refreshed?.Select();
            return refreshed?.Identifier.ID == target?.Identifier.ID;
        }
        /// <summary>
        /// This method returns the frame type
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private string GetFrameType(Assembly assembly)
        {
            string type = "";
            assembly.GetUserProperty("GBS_FRAME_TYPE", ref type);
            return type ?? "";
        }
        /// <summary>
        /// Used to check if the girt is parallel with the column. > ND-3685 and ND-2423
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="assemblies"></param>
        /// <param name="leftBayType"></param>
        /// <returns></returns>
        public bool IsParallelGirtWithColumn(TeklaPurlinGirtConnection connection, IEnumerable<Assembly> assemblies, PlaneTypeEnum leftBayType)
        {
            var filteredAssemblies = assemblies.Where(a => !GetFrameType(a).ToUpper().Contains("PORTAL")).ToList();

            var leftGirt = connection?.LeftPurlinGirt?.Beam;
            var rightGirt = connection?.RightPurlinGirt?.Beam;

            var refreshedLeftGirt = leftGirt != null ? TeklaProject.Model.SelectModelObject(leftGirt.Identifier) as Beam : null;
            var refreshedRightGirt = rightGirt != null ? TeklaProject.Model.SelectModelObject(rightGirt.Identifier) as Beam : null;
            refreshedLeftGirt?.Select();
            refreshedRightGirt?.Select();

            Assembly requiredAssembly = null;
            if (leftBayType == PlaneTypeEnum.Roof)
            {
                Beam girt = refreshedRightGirt ?? refreshedLeftGirt;
                Point girtCenter = girt?.GetSolid()?.GetExtentsCenter();
                requiredAssembly = filteredAssemblies
                    .Where(a => a.IsRafterAssembly())
                    .OrderBy(a =>
                    {
                        Beam part = a.GetMainObject()?.GetFatherComponent()?.GetParts()?.FirstOrDefault(p => (p as Beam)?.IsInsideFlange() == true) as Beam;
                        return part == null ? double.MaxValue : Distance.PointToPoint(part.GetSolid()?.GetExtentsCenter(), girtCenter);
                    }).FirstOrDefault();
            }
            else
            {
                requiredAssembly = filteredAssemblies.FirstOrDefault(a => a.IsColumnAssembly());
            }

            if (!(requiredAssembly?.GetMainObject() is Part columnOrRafter)) return false;

            GeometricPlane insidePlane = null, outsidePlane = null;
            if (columnOrRafter.IsFlatPlate())
            {
                var parts = columnOrRafter.GetFatherComponent().GetParts();
                Beam inside = parts.OfType<Beam>().FirstOrDefault(b => b.IsInsideFlange());
                Beam outside = parts.OfType<Beam>().FirstOrDefault(b => b.IsOutsideFlange());
                insidePlane = inside != null ? new GeometricPlane(inside.GetCoordinateSystem()) : null;
                outsidePlane = outside != null ? new GeometricPlane(outside.GetCoordinateSystem()) : null;
            }
            else if (columnOrRafter.IsHotRolledIBeam())
            {
                CoordinateSystem cs = (columnOrRafter as Beam).GetCoordinateSystem();
                insidePlane = outsidePlane = new GeometricPlane(cs.Origin, cs.AxisY.GetNormal());
            }

            bool IsParallel(Beam girt)
            {
                if (girt == null || insidePlane == null || outsidePlane == null) return false;
                var girtPlane = new GeometricPlane(girt.StartPoint, girt.GetCoordinateSystem().AxisY.GetNormal());
                return Parallel.PlaneToPlane(insidePlane, girtPlane) || Parallel.PlaneToPlane(outsidePlane, girtPlane);
            }
            return IsParallel(refreshedLeftGirt) || IsParallel(refreshedRightGirt);
        }


        private List<TeklaPurlinGirt> GetZeeConnectGirts(List<TeklaPurlinGirt> bayGirts, List<TeklaPurlinGirtConnection> connections)
        {
            List<TeklaPurlinGirt> requiredGirts = new List<TeklaPurlinGirt>();

            // making sure that only girts which belongs to Zee-Connect gets added to the list and ignore other connection girts.
            foreach (TeklaPurlinGirtConnection connection in connections)
            {
                foreach (var girt in bayGirts)
                {
                    if (connection.LeftPosition != null)
                    {
                        if (connection.LeftPurlinGirt.Beam.Identifier == girt.Beam.Identifier)
                            requiredGirts.Add(connection.LeftPurlinGirt);
                    }

                    if (connection.RightPosition != null)
                    {
                        if (connection.RightPurlinGirt.Beam.Identifier == girt.Beam.Identifier)
                            requiredGirts.Add(connection.RightPurlinGirt);
                    }
                }
            }
            return requiredGirts;
        }
        /// <summary>
        /// This method will return sidewall or endwall based n column orientation
        /// if corner column is rotated column method will return Endwall
        /// if corner column is not rotated column method will return Sidewall
        /// <param name="leftBay"></param>
        /// <param name="leftBayType"></param>
        /// <param name="rightBay"></param>
        /// <param name="rightBayType"></param>
        /// <param name="assemblies"></param>
        /// <param name="cornerColumn"></param>
        /// <returns></returns>
        private (TeklaBay, bool) DetermineRequiredTeklaBay(TeklaBay leftBay, PlaneTypeEnum leftBayType, TeklaBay rightBay, PlaneTypeEnum rightBayType, IEnumerable<Assembly> assemblies, out ModelObject cornerColumn)
        {
            cornerColumn = null;
            var model = new Model();

            if (leftBayType != PlaneTypeEnum.Roof)
            {
                // Find the required corner column that is NOT an end wall column

                var filteredAssemblies = assemblies.Where(a => !GetFrameType(a).ToUpper().Contains("PORTAL")).ToList();
                var requiredAssembly = filteredAssemblies.FirstOrDefault(a => a.IsColumnAssembly());

                if (requiredAssembly == null)
                    return (null, false);

                cornerColumn = requiredAssembly.GetMainObject();
                bool isRotatedCol = requiredAssembly.CheckRotatedColumn(model);

                if (!isRotatedCol)
                {
                    if (leftBayType == PlaneTypeEnum.Sidewall) return (leftBay, false);
                    if (rightBayType == PlaneTypeEnum.Sidewall) return (rightBay, false);
                }
                else
                {
                    if (leftBayType == PlaneTypeEnum.Endwall) return (leftBay, true);
                    if (rightBayType == PlaneTypeEnum.Endwall) return (rightBay, true);
                }
            }
            else
            {
                if (leftBayType == PlaneTypeEnum.Roof && leftBay != null) return (leftBay, false);
                if (rightBayType == PlaneTypeEnum.Roof && rightBay != null) return (rightBay, false);
            }
            return (null, false);
        }

        #endregion
