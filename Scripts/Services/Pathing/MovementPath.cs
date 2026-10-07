using System;
using Server.Commands;
using Server.Items;
using Server.PathAlgorithms;
using Server.PathAlgorithms.FastAStar;
using Server.PathAlgorithms.SlowAStar;
using Server.Spells;
using Server.Targeting;

namespace Server
{
    public sealed class MovementPath
    {
        public static PathAlgorithm OverrideAlgorithm { get; set; }

        public Map Map { get; }

        public Point3D Start { get; }

        public Point3D Goal { get; }

        public Direction[] Directions { get; }

        public bool Success => Directions != null && Directions.Length > 0;

        public MovementPath(Mobile m, Point3D goal)
            : this(m, goal, m.Map)
        {
        }

        public MovementPath(IPoint3D p, Point3D goal, Map map)
        {
            var start = new Point3D(p);

            Map = map;
            Start = start;
            Goal = goal;

            if (map == null || map == Map.Internal)
                return;

            if (Utility.InRange(start, goal, 1))
                return;

            try
            {
                var alg = OverrideAlgorithm;

                if (alg == null) alg = FastAStarAlgorithm.Instance;
                //if ( !alg.CheckCondition( m, map, start, goal ) )	// SlowAstar is still broken
                //	alg = SlowAStarAlgorithm.Instance;		// TODO: Fix SlowAstar
                if (alg != null && alg.CheckCondition(p, map, start, goal))
                    Directions = alg.Find(p, map, start, goal);
            }
            catch (Exception e)
            {
                Console.WriteLine("Warning: {0}: Pathing error from {1} to {2}", e.GetType().Name, start, goal);
                Console.WriteLine(e.StackTrace);
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("Path", AccessLevel.GameMaster, Path_OnCommand);
        }

        public static void Path_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, true, TargetFlags.None, Path_OnTarget);
            e.Mobile.SendMessage("Target a location and a path will be drawn there.");
        }

        public static void Path_OnTarget(Mobile from, object obj)
        {
            var p = obj as IPoint3D;

            if (p == null)
                return;

            SpellHelper.GetSurfaceTop(ref p);

            Path(from, p, FastAStarAlgorithm.Instance, "Fast", 0);
            Path(from, p, SlowAStarAlgorithm.Instance, "Slow", 2);
            OverrideAlgorithm = null;
            /*MovementPath path = new MovementPath( from, new Point3D( p ) );
            if ( !path.Success )
            {
            from.SendMessage( "No path to there could be found." );
            }
            else
            {
            //for ( int i = 0; i < path.Directions.Length; ++i )
            //	Timer.DelayCall( TimeSpan.FromSeconds( 0.1 + (i * 0.3) ), new TimerStateCallback( Pathfind ), new object[]{ from, path.Directions[i] } );
            int x = from.X;
            int y = from.Y;
            int z = from.Z;
            for ( int i = 0; i < path.Directions.Length; ++i )
            {
            Movement.Movement.Offset( path.Directions[i], ref x, ref y );
            new Items.RecallRune().MoveToWorld( new Point3D( x, y, z ), from.Map );
            }
            }*/
        }

        public static void Pathfind(object state)
        {
            var states = (object[])state;
            var from = (Mobile)states[0];
            var d = (Direction)states[1];

            try
            {
                from.Direction = d;
                from.NetState.BlockAllPackets = true;
                from.Move(d);
                from.NetState.BlockAllPackets = false;
                from.ProcessDelta();
            }
            catch
            {
            }
        }

        private static void Path(Mobile from, IPoint3D p, PathAlgorithm alg, string name, int zOffset)
        {
            OverrideAlgorithm = alg;

            var start = DateTime.UtcNow.Ticks;
            var path = new MovementPath(from, new Point3D(p));
            var end = DateTime.UtcNow.Ticks;
            var len = Math.Round((end - start) / 10000.0, 2);

            if (!path.Success)
            {
                from.SendMessage("{0} path failed: {1}ms", name, len);
            }
            else
            {
                from.SendMessage("{0} path success: {1}ms", name, len);

                var x = from.X;
                var y = from.Y;
                var z = from.Z;

                for (var i = 0; i < path.Directions.Length; ++i)
                {
                    Movement.Movement.Offset(path.Directions[i], ref x, ref y);

                    new RecallRune().MoveToWorld(new Point3D(x, y, z + zOffset), from.Map);
                }
            }
        }
    }
}
