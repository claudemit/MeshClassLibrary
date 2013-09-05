﻿using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Collections;

using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.Geometry.Collections;
namespace MeshClassLibrary
{
    public class MeshTools
    {
        public MeshTools() { }
        #region mesh functions

        /// MeshCreation
        public Mesh RuledMesh(Curve c1, Curve c2, int t)
        {
            return RuledMesh(c1, c2, t, t);
        }
        public Mesh RuledMesh(Curve c1, Curve c2, int u, int v)
        {
            double[] uList1 = c1.DivideByCount(u, true);
            double[] uList2 = c2.DivideByCount(u, true);
            List<Polyline> input1 = new List<Polyline>();
            for (int i = 0; i < uList1.Length; i++)
            {
                Point3d p1 = c1.PointAt(uList1[i]);
                Point3d p2 = c2.PointAt(uList2[i]);
                Vector3d V = p2 - p1;
                V /= v;
                Polyline pl = new Polyline();
                for (int k = 0; k < v + 1; k++)
                {
                    pl.Add(p1 + (V * k));
                }
                input1.Add(pl);
            }
            return MeshLoft(input1, false, false);
        }
        public Mesh RuledMesh(Curve c1, Point3d c2, int t)
        {
            List<Point3d> ps = new List<Point3d>();
            ps.Add(c2);
            for (int i = 2; i < t + 2; i++)
            {
                double[] t1 = c1.DivideByCount(i - 1, true);
                for (int k = 0; k < t1.Length; k++)
                {
                    Vector3d v = c1.PointAt(t1[k]) - c2;
                    v *= ((double)i - 1) / (double)t;
                    ps.Add(c2 + v);
                }
            }
            return MeshFromPoints(ps, t + 1);
        }
        public void MeshClean(ref Mesh mesh, double tolerance)
        {
            try
            {
                List<int> mapping = new List<int>();
                List<Point3d> ps = new List<Point3d>();
                List<double> MapCount = new List<double>();
                ps.Add(mesh.Vertices[0]); mapping.Add(0); MapCount.Add(1);
                for (int j = 1; j < mesh.Vertices.Count; j++)
                {
                    double min = double.MaxValue;
                    int sign = 0;

                    for (int i = 0; i < ps.Count; i++)
                    {
                        double tempt = ps[i].DistanceTo((Point3d)mesh.Vertices[j]);
                        if (tempt < min)
                        {
                            min = tempt; sign = i;
                        }
                    }
                    if (min < tolerance)
                    {
                        mapping.Add(sign);
                        MapCount[sign]++;           
                        ps[sign] *= (MapCount[sign] - 1) / MapCount[sign];                    
                        Point3d tempp = (Point3d)(mesh.Vertices[j]); tempp *= (1 / MapCount[sign]);
                        ps[sign] += tempp;                   
                    }
                    else { mapping.Add(ps.Count); ps.Add(mesh.Vertices[j]); MapCount.Add(1); }
                }
                Mesh mesh2 = new Mesh();
                for (int i = 0; i < ps.Count; i++)
                {
                    mesh2.Vertices.Add(ps[i]);
                }
                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    if (mesh.Faces[i].IsQuad)
                    {
                        int p1 = mapping[mesh.Faces[i].A];
                        int p2 = mapping[mesh.Faces[i].B];
                        int p3 = mapping[mesh.Faces[i].C];
                        int p4 = mapping[mesh.Faces[i].D];
                        if (noRepeat(p1, p2, p3, p4))
                        {
                            mesh2.Faces.AddFace(p1, p2, p3, p4);
                        }
                    }
                    if (mesh.Faces[i].IsTriangle)
                    {
                        int p1 = mapping[mesh.Faces[i].A];
                        int p2 = mapping[mesh.Faces[i].B];
                        int p3 = mapping[mesh.Faces[i].C];
                        if (noRepeat(p1, p2, p3))
                        {
                            mesh2.Faces.AddFace(p1, p2, p3);
                        }
                    }
                }
                mesh2.Compact();
                mesh2.UnifyNormals();
                mesh = mesh2;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }
        private bool noRepeat(int a1, int a2, int a3, int a4)
        {
            if (a1 == a2) return false;
            else if (a1 == a3) return false;
            else if (a1 == a4) return false;
            else if (a2 == a3) return false;
            else if (a2 == a4) return false;
            else if (a3 == a4) return false;
            else return true;
        }
        private bool noRepeat(int a1, int a2, int a3)
        {
            if (a1 == a2) return false;
            else if (a1 == a3) return false;
            else if (a2 == a3) return false;
            else return true;
        }
        public List<Line> MeshEdge(Mesh mesh)
        {
            List<Line> ls = new List<Line>();
            MeshTopologyEdgeList el=mesh.TopologyEdges;
            for(int i=0;i<el.Count;i++){
                if(el.GetConnectedFaces(i).Length!=2)
                    ls.Add(el.EdgeLine(i));
                }
            return ls;
        }
        public Mesh MeshTorus(Circle c, double t)
        {
            double cut = 64;
            List<Polyline> ls2 = new List<Polyline>();
            for (int i = 0; i < cut; i++)
            {
                List<Point3d> ls = new List<Point3d>();
                ls.Add(new Point3d(1, 0, 0));
                ls.Add(new Point3d(0.707, 0, 0.707));
                ls.Add(new Point3d(0, 0, 1));
                ls.Add(new Point3d(-0.707, 0, 0.707));
                ls.Add(new Point3d(-1, 0, 0));
                ls.Add(new Point3d(-0.707, 0, -0.707));
                ls.Add(new Point3d(0, 0, -1));
                ls.Add(new Point3d(0.707, 0, -0.707));
                Polyline l1 = new Polyline();
                l1.AddRange(ls);
                l1.Reverse();
                l1.Transform(Transform.Scale(Point3d.Origin, t));
                //l1.Transform(Transform.Translation(new Vector3d(c.Radius - t, 0, 0)));
                l1.Transform(Transform.Translation(new Vector3d(c.Radius, 0, 0)));
                l1.Transform(Transform.Rotation(Math.PI / (double)cut * 2 * i, Point3d.Origin));
                ls2.Add(l1);
            }
            Mesh mesh1 = MeshLoft(ls2, true, true);
            mesh1.Transform(Transform.Translation(new Vector3d(c.Center)));
            return mesh1;
        }
        public Mesh MeshSweep1(Curve l, Polyline ls, Plane plane, int Count)
        {
            List<Polyline> ps = new List<Polyline>();
            Mesh mesh = new Mesh();
            double[] div = l.DivideByCount(Count, true);
            for (int i = 0; i < div.Length; i++)
            {
                Polyline l1 = new Polyline(ls);
                if (l.PerpendicularFrameAt(div[i], out plane))
                {
                    l1.Transform(Transform.PlaneToPlane(Plane.WorldXY, plane));
                    ps.Add(l1);
                }
            }
            mesh.Append(MeshLoft(ps, true, false));
            return mesh;
        }
        public Mesh MeshPipe(Curve l, double t, int Count)
        {
            Mesh mesh = new Mesh();
            List<Point3d> ls = new List<Point3d>();
            ls.Add(new Point3d(1, 0, 0));
            ls.Add(new Point3d(0.707, 0.707, 0));
            ls.Add(new Point3d(0, 1, 0));
            ls.Add(new Point3d(-0.707, 0.707, 0));
            ls.Add(new Point3d(-1, 0, 0));
            ls.Add(new Point3d(-0.707, -0.707, 0));
            ls.Add(new Point3d(0, -1, 0));
            ls.Add(new Point3d(0.707, -0.707, 0));
            Polyline l1 = new Polyline(ls);
            l1.Transform(Transform.Scale(Point3d.Origin, t));
            return MeshSweep1(l, l1, Plane.WorldXY, Count);
        }
        public Mesh MeshPipe(Line l, double t)
        {
            Mesh mesh = new Mesh();
            List<Point3d> ls = new List<Point3d>();
            ls.Add(new Point3d(1, 0, 0));
            ls.Add(new Point3d(0.707, 0.707, 0));
            ls.Add(new Point3d(0, 1, 0));
            ls.Add(new Point3d(-0.707, 0.707, 0));
            ls.Add(new Point3d(-1, 0, 0));
            ls.Add(new Point3d(-0.707, -0.707, 0));
            ls.Add(new Point3d(0, -1, 0));
            ls.Add(new Point3d(0.707, -0.707, 0));
            Polyline l1 = new Polyline(ls);
            Polyline l2 = new Polyline(ls);
            l1.Transform(Transform.Scale(new Point3d(0, 0, 0), t));
            l2.Transform(Transform.Scale(new Point3d(0, 0, 0), t));
            Vector3d v = l.To - l.From;
            l1.Transform(Transform.PlaneToPlane(Plane.WorldXY, new Plane(l.From, v)));
            l2.Transform(Transform.PlaneToPlane(Plane.WorldXY, new Plane(l.To, v)));
            mesh.Append(MeshLoft(l1, l2, true, false));
            return mesh;
        }
        public Mesh MeshBeam(Line Line, double height, double width, Vector3d N)
        {
            List<Point3d> ps = new List<Point3d>();
            ps.Add(new Point3d(height, width, 0));
            ps.Add(new Point3d(-height, width, 0));
            ps.Add(new Point3d(-height, -width, 0));
            ps.Add(new Point3d(height, -width, 0));
            Polyline l1 = new Polyline(ps);
            Polyline l2 = new Polyline(ps);
            Plane ori = Plane.WorldXY;
            Vector3d vy = Line.To - Line.From;
            Vector3d vx = Vector3d.CrossProduct(vy, N);

            Plane plane1 = new Plane(Line.From, N, vx);
            Plane plane2 = new Plane(Line.To, N, vx);
            l1.Transform(Transform.PlaneToPlane(ori, plane1));
            l2.Transform(Transform.PlaneToPlane(ori, plane2));
            Mesh mesh = new Mesh();
            mesh.Vertices.AddVertices(l1);
            mesh.Vertices.AddVertices(l2);
            mesh.Faces.AddFace(0, 1, 2, 3);
            mesh.Faces.AddFace(4, 5, 6, 7);
            mesh.Append(MeshLoft(l1, l2, true, false));
            return mesh;
        }
        public Mesh MeshLoft(Polyline pl1, Polyline pl2, bool isPolyClosed, bool isClosed)
        {

            List<Polyline> pls = new List<Polyline>();
            pls.Add(pl1);
            pls.Add(pl2);
            return MeshLoft(pls, isPolyClosed, isClosed);
        }
        public Mesh MeshLoft(List<Polyline> pl, bool isPolyClosed, bool isClosed)
        {
            int U = pl[0].Count;
            int V = pl.Count;
            if (isPolyClosed) { U++; }
            if (isClosed) { pl.Add(pl[0]); V++; }
            List<Point3d> pls = new List<Point3d>();

            for (int i = 0; i < pl.Count; i++)
            {
                for (int j = 0; j < pl[i].Count; j++)
                {
                    pls.Add(pl[i][j]);
                }
                if (isPolyClosed) { pls.Add(pl[i][0]); }
            }
            return MeshFromPoints(pls, U, V);
        }
        public Mesh MeshLoft(Line l1, Line l2)
        {
            return MeshFromPoints(l1.From, l1.To, l2.To, l2.From);
        }
        public Mesh MeshLoft(List<Line> ls,bool isClosed){
            Polyline l1 = new Polyline(), l2 = new Polyline();
            for (int i = 0; i < ls.Count; i++)
            {
                l1.Add(ls[i].From); l2.Add(ls[i].To);
            }
            return MeshLoft(l1, l2, isClosed, false);
        }
        public Mesh MeshExtrute(Mesh meshOral, Vector3d v)
        {
            Mesh mesh = new Mesh();
            meshOral.UnifyNormals();
            meshOral.Compact();
            for (int i = 0; i < meshOral.Vertices.Count; i++)
            {
                mesh.Vertices.Add(new Point3d(meshOral.Vertices[i]) + v);
            }
            for (int i = 0; i < meshOral.Faces.Count; i++)
            {
                mesh.Faces.AddFace(meshOral.Faces[i]);
            }
            Rhino.Geometry.Collections.MeshTopologyEdgeList el = meshOral.TopologyEdges;
            for (int i = 0; i < el.Count; i++)
            {
                if (el.GetConnectedFaces(i).Length == 1)
                {
                    int VS = mesh.Vertices.Count;
                    mesh.Vertices.Add(el.EdgeLine(i).From);
                    mesh.Vertices.Add(el.EdgeLine(i).To);
                    mesh.Vertices.Add(el.EdgeLine(i).To + v);
                    mesh.Vertices.Add(el.EdgeLine(i).From + v);
                    mesh.Faces.AddFace(VS, VS + 1, VS + 2, VS + 3);
                }
            }
            mesh.Normals.ComputeNormals();
            mesh.Append(meshOral);
            mesh.UnifyNormals();
            return mesh;
        }
        public Mesh MeshExtrute(Mesh meshOral, double v)
        {
            Mesh mesh = new Mesh();
            meshOral.UnifyNormals();
            meshOral.Compact();
            for (int i = 0; i < meshOral.Vertices.Count; i++)
            {
                mesh.Vertices.Add(new Point3d(meshOral.Vertices[i]) + new Vector3d(meshOral.Normals[i]) * v);
            }
            for (int i = 0; i < meshOral.Faces.Count; i++)
            {
                mesh.Faces.AddFace(meshOral.Faces[i]);
            }
            Rhino.Geometry.Collections.MeshTopologyEdgeList el = meshOral.TopologyEdges;
            Rhino.Geometry.Collections.MeshTopologyVertexList vs = meshOral.TopologyVertices;
            for (int i = 0; i < el.Count; i++)
            {
                if (el.GetConnectedFaces(i).Length == 1)
                {
                    int p1 = el.GetTopologyVertices(i).I;
                    int p2 = el.GetTopologyVertices(i).J;
                    Vector3d v1 = new Vector3d(meshOral.Normals[vs.MeshVertexIndices(p1)[0]]);
                    Vector3d v2 = new Vector3d(meshOral.Normals[vs.MeshVertexIndices(p2)[0]]);
                    int VS = mesh.Vertices.Count;
                    mesh.Vertices.Add(vs[p1]);
                    mesh.Vertices.Add(vs[p2]);
                    mesh.Vertices.Add(new Point3d(vs[p2]) + v2 * v);
                    mesh.Vertices.Add(new Point3d(vs[p1]) + v1 * v);
                    mesh.Faces.AddFace(VS, VS + 1, VS + 2, VS + 3);
                }
            }
            mesh.Normals.ComputeNormals();
            mesh.Append(meshOral);
            mesh.UnifyNormals();
            return mesh;
        }
        public Mesh MeshFromPoints(List<Point3d> pl)
        {
            //triangle MeshTopo Points From topo of the pyramid to the base
            double t = pl.Count;
            double l = Math.Sqrt(t * 8 + 1) - 1;
            l /= 2;
            return MeshFromPoints(pl, (int)l);
        }
        public Mesh MeshFromPoints(List<Point3d> pl, int t)
        {
            //triangle MeshTopo Points From topo of the pyramid to the base
            Mesh mesh = new Mesh();
            if (t < 2) return mesh;
            int n = ((1 + t) * t) / 2;
            if (n > pl.Count) return mesh;

            mesh.Vertices.AddVertices(pl);
            List<int> layer1; List<int> layer2 = new List<int>();
            layer2.Add(0);
            for (int i = 0; i < t - 1; i++)
            {
                layer1 = new List<int>(layer2);
                for (int j = 0; j < layer2.Count; j++)
                {
                    layer2[j] += i + 1;
                }
                layer2.Add(layer2[layer2.Count - 1] + 1);


                if (layer1.Count > 1)
                {
                    for (int j = 0; j < layer1.Count - 1; j++)
                    {
                        mesh.Faces.AddFace(layer1[j], layer1[j + 1], layer2[j + 1]);
                    }
                }
                for (int j = 0; j < layer1.Count; j++)
                {
                    mesh.Faces.AddFace(layer2[j], layer1[j], layer2[j + 1]);
                }
            }
            mesh.Compact();
            mesh.Normals.ComputeNormals();
            return mesh;
        }
        public Mesh MeshFromPoints(List<Point3d> pl, int u, int v)
        {
            if (u * v > pl.Count || u < 2 || v < 2) return null;
            Mesh mesh = new Mesh();
            for (int i = 0; i < pl.Count; i++)
            {
                mesh.Vertices.Add(pl[i]);
            }
            for (int i = 1; i < u; i++)
            {
                for (int j = 1; j < v; j++)
                {
                    mesh.Faces.AddFace(new MeshFace(
                    (j - 1) * u + i - 1,
                    (j - 1) * u + i,
                    (j) * u + i,
                    (j) * u + i - 1));
                }
            }
            mesh.Normals.ComputeNormals();
            return mesh;
        }
        public Mesh MeshFromPoints(Point3d p1, Point3d p2, Point3d p3, Point3d p4)
        {
            Mesh mesh = new Mesh();
            mesh.Vertices.Add(p1);
            mesh.Vertices.Add(p2);
            mesh.Vertices.Add(p3);
            mesh.Vertices.Add(p4);
            mesh.Faces.AddFace(0, 1, 2, 3);
            mesh.Normals.ComputeNormals();
            return mesh;
        }
        public Mesh MeshFromPoints(Point3d p1, Point3d p2, Point3d p3)
        {
            Mesh mesh = new Mesh();
            mesh.Vertices.Add(p1);
            mesh.Vertices.Add(p2);
            mesh.Vertices.Add(p3);
            mesh.Faces.AddFace(0, 1, 2);
            mesh.Normals.ComputeNormals();
            return mesh;
        }

        /// RemeshFunctions
        public Mesh MeshFromClosedPoly(List<Polyline> x)
        {
            Mesh mesh = new Mesh();
            for (int i = 0; i < x.Count; i++)
            {
                if (x[i].Count == 4)
                {
                    int n = mesh.Vertices.Count;
                    mesh.Vertices.Add(x[i][0]);
                    mesh.Vertices.Add(x[i][1]);
                    mesh.Vertices.Add(x[i][2]);
                    mesh.Faces.AddFace(new MeshFace(n, n + 1, n + 2));
                }
                else if (x[i].Count == 5)
                {
                    int n = mesh.Vertices.Count;
                    mesh.Vertices.Add(x[i][0]);
                    mesh.Vertices.Add(x[i][1]);
                    mesh.Vertices.Add(x[i][2]);
                    mesh.Vertices.Add(x[i][3]);
                    mesh.Faces.AddFace(new MeshFace(n, n + 1, n + 2, n + 3));
                }
            }
            mesh.Normals.ComputeNormals();
          return mesh;
        }
        public List<Polyline> Remesh(List<Line> x, Vector3d y)
        {
            List<M_Point> PointList = remesh_preparations(x);
            for (int i = 0; i < PointList.Count; i++)
            {    
                PointList[i].computeNormal(y);
                PointList[i].lay();
            }
            return remesh_subsequent(PointList);
        }
        public List<Polyline> Remesh(List<Line> x, Surface y)
        {
            List<M_Point> PointList = remesh_preparations(x);
                for (int i = 0; i < PointList.Count; i++)
                {
                    PointList[i].computeNormal(y);
                    PointList[i].lay();
                }
                return remesh_subsequent(PointList);
        }
        public List<Polyline> Remesh(List<Line> x, Mesh y)
        {
            List<M_Point> PointList = remesh_preparations(x);
            for (int i = 0; i < PointList.Count; i++)
            {
                PointList[i].computeNormal(y);
                PointList[i].lay();
            }
            return remesh_subsequent(PointList);
        }
        public List<Polyline> Remesh(List<Line> x)
        {
            List<M_Point> PointList = remesh_preparations(x);
            for (int i = 0; i < PointList.Count; i++)
            {
                PointList[i].computeNormal();
                PointList[i].lay();
            }
            return remesh_subsequent(PointList);
        }
        public List<Polyline> Remesh(List<Line> x, List<Vector3d> y)
        {
            List<M_Point> PointList = remesh_preparations(x);
            for (int i = 0; i < PointList.Count; i++)
            {
                int iy = i;
                if (iy > y.Count - 1) iy = y.Count - 1;
                PointList[i].computeNormal(y[iy]);
                PointList[i].lay();
            }
            return remesh_subsequent(PointList);
        }
        private List<M_Point> remesh_preparations(List<Line> x)
        {
              List<M_Point> PointList = new List<M_Point>();
                PointList.Add(new M_Point(x[0].From));
                PointList.Add(new M_Point(x[0].To));
                PointList[0].refpoints.Add(PointList[1]);
                PointList[1].refpoints.Add(PointList[0]);
                for (int i = 1; i < x.Count; i++)
                {
                    bool sign1 = true;
                    bool sign2 = true;
                    int C1 = -1; int C2 = -1;
                    M_Point P1 = new M_Point(x[i].From);
                    M_Point P2 = new M_Point(x[i].To);
                    for (int j = 0; j < PointList.Count; j++)
                    {
                        if (sign1)
                        {
                            if (PointList[j].isDump(P1)) { sign1 = false; C1 = j; }
                        }
                        if (sign2)
                        {
                            if (PointList[j].isDump(P2)) { sign2 = false; C2 = j; }
                        }
                        if (sign1 == false && sign2 == false) break;
                    }
                    if (sign1) { PointList.Add(P1); C1 = PointList.Count - 1; }
                    if (sign2) { PointList.Add(P2); C2 = PointList.Count - 1; }
                    PointList[C1].refpoints.Add(PointList[C2]);
                    PointList[C2].refpoints.Add(PointList[C1]);
                }
            return PointList;
        }
        private List<Polyline> remesh_subsequent(List<M_Point> PointList)
        {
            List<Polyline> pls = new List<Polyline>();
            for (int i = 0; i < PointList.Count; i++)
            {
                for (int j = 0; j < PointList[i].children.Count; j++)
                {
                    M_Point Pt = PointList[i].children[j];
                    if (Pt.order == 0)
                    {
                        Polyline pl = new Polyline();
                        int or = 0;
                        pl.Add(Pt.pos); Pt.order = 1;
                        for (int ii = 0; ii < PointList.Count; ii++)
                        {
                            bool signii = true;
                            M_Point pt2 = Pt.refpoints[or]; if (or == 0) { or = 1; } else { or = 0; }
                            for (int jj = 0; jj < pt2.children.Count; jj++)
                            {
                                if (pt2.children[jj].order == 0)
                                {
                                    if (pt2.children[jj].refpoints[or].isDump(Pt))
                                    {
                                        Pt = pt2.children[jj];
                                        pl.Add(Pt.pos); Pt.order = 1;
                                        if (or == 0) { or = 1; } else { or = 0; }
                                        signii = false;
                                        break;
                                    }
                                }
                            }
                            if (signii) { break; }
                        }
                        if (pl.Count > 2) { pl.Add(pl[0]); pls.Add(pl); }
                    }
                }
            }
            return pls;
        }

        /// 2Nurbs 
        public string Mesh2Nurbs(Mesh mesh, out  NurbsSurface s)
        {
            Rhino.Geometry.Collections.MeshTopologyEdgeList el = mesh.TopologyEdges;
            Rhino.Geometry.Collections.MeshTopologyVertexList vs = mesh.TopologyVertices;
            string str = "";
            List<int> firstLoop1;
            str += FirstEdge(mesh, out firstLoop1);
            double column = (double)vs.Count / (double)firstLoop1.Count;
            int Column = (int)column;
            if (column - Column != 0) str += "Points Count error,Please confirm the topo is quad";
            int[] energy = new int[vs.Count];
            List<int> indexPt = new List<int>();
            indexPt.AddRange(firstLoop1);
            for (int i = 0; i < firstLoop1.Count; i++) { energy[firstLoop1[i]] = 1; }
            for (int i = 0; i < Column - 1; i++)
            {
                bool sign = true;
                for (int j = 0; j < firstLoop1.Count; j++)
                {
                    int[] index = vs.ConnectedTopologyVertices(firstLoop1[j]);
                    for (int k = 0; k < index.Length; k++)
                    {
                        //Print("j:" + j.ToString() + " k:" + k.ToString() + " energy: " + energy[index[k]].ToString());////////
                        energy[index[k]]++;
                    }
                }
                //Print("///");
                for (int j = 0; j < firstLoop1.Count; j++)
                {

                    int[] index = vs.ConnectedTopologyVertices(firstLoop1[j]);
                    for (int k = 0; k < index.Length; k++)
                    {
                        // Print("j:" + j.ToString() + " k:" + k.ToString() + " energy: " + energy[index[k]].ToString());////////
                        if (energy[index[k]] == 1)
                        {
                            firstLoop1[j] = index[k]; sign = false; break;
                        }
                    }
                }
                if (sign) { str += " Loop false,Not a quad topo Or To the end"; }
                else { indexPt.AddRange(firstLoop1); }
            }
            List<Point3d> output1 = new List<Point3d>();
            for (int i = 0; i < indexPt.Count; i++)
            {
                output1.Add(vs[indexPt[i]]);
            }
            s = NurbsSurface.CreateFromPoints(output1, Column, firstLoop1.Count, 3, 3);
            return str;
        }
        public string FirstEdge(Mesh mesh, out  List<int> firstLoop1)
        {
            Rhino.Geometry.Collections.MeshTopologyEdgeList el = mesh.TopologyEdges;
            Rhino.Geometry.Collections.MeshTopologyVertexList vs = mesh.TopologyVertices;
            firstLoop1 = new List<int>();
            int firstPoint = -1;
            for (int i = 0; i < vs.Count; i++)
            {
                if (vs.ConnectedTopologyVertices(i).Length == 2)
                {
                    firstPoint = i; break;
                }
            }
            if (firstPoint == -1) { return "can not find a 2 degree Vertex,Please check the mesh boundary"; }
            int SecondPoint = vs.ConnectedTopologyVertices(firstPoint)[0];
            int ThirdPoint = vs.ConnectedTopologyVertices(firstPoint)[1];
            int firstPoint1 = firstPoint;
            firstLoop1.Add(firstPoint);



            if (vs.ConnectedTopologyVertices(ThirdPoint).Length == 2)
            {
                firstLoop1.Add(ThirdPoint);
            }
            else if (vs.ConnectedTopologyVertices(SecondPoint).Length == 2)
            {
                firstLoop1.Add(SecondPoint);
            }
            else
            {
                firstLoop1.Add(SecondPoint);
                for (int i = 0; i < vs.Count; i++)
                {
                    bool stop = false;
                    int[] index = vs.ConnectedTopologyVertices(SecondPoint);
                    for (int j = 0; j < index.Length; j++)
                    {
                        if (index[j] != firstPoint1)
                        {
                            if (vs.ConnectedTopologyVertices(index[j]).Length == 3)
                            {
                                firstPoint1 = SecondPoint;
                                SecondPoint = index[j];
                                firstLoop1.Add(SecondPoint);
                                break;
                            }//if
                            if (vs.ConnectedTopologyVertices(index[j]).Length == 2)
                            {
                                firstPoint1 = SecondPoint;
                                SecondPoint = index[j];
                                firstLoop1.Add(SecondPoint);
                                stop = true;
                                break;
                            }//if
                        }
                    }
                    if (stop) break;
                }
            }
            return "Done";
        }
        #endregion
    }
}
