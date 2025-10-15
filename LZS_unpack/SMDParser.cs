using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using APPLIB;

namespace LZS_unpack
{
	/// <summary>
	/// Parser for Source Engine SMD files
	/// </summary>
	internal class SMDParser
	{
		public class Bone
		{
			public int Id;
			public string Name;
			public int ParentId;
			public Vector3D Position;
			public Vector3D Rotation;
		}

		public class Triangle
		{
			public string Material;
			public Vertex[] Vertices = new Vertex[3];
		}

		public class Vertex
		{
			public int BoneId;
			public Vector3D Position;
			public Vector3D Normal;
			public float U;
			public float V;
			public List<BoneWeight> Weights = new List<BoneWeight>();
		}

		public class BoneWeight
		{
			public int BoneId;
			public float Weight;
		}

		public List<Bone> Bones = new List<Bone>();
		public List<Triangle> Triangles = new List<Triangle>();

		public void Parse(string filePath)
		{
			NumberFormatInfo nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ".";

			StreamReader sr = new StreamReader(filePath);
			string line;
			string currentSection = "";

			while ((line = sr.ReadLine()) != null)
			{
				line = line.Trim();
				if (line == "") continue;

				if (line == "version 1") continue;
				if (line == "nodes") { currentSection = "nodes"; continue; }
				if (line == "skeleton") { currentSection = "skeleton"; continue; }
				if (line == "triangles") { currentSection = "triangles"; continue; }
				if (line == "end") { currentSection = ""; continue; }

				if (currentSection == "nodes")
				{
					ParseBoneNode(line);
				}
				else if (currentSection == "skeleton")
				{
					if (line.StartsWith("time")) continue;
					ParseBonePose(line, nfi);
				}
				else if (currentSection == "triangles")
				{
					ParseTriangle(ref sr, line, nfi);
				}
			}
			sr.Close();
		}

		private void ParseBoneNode(string line)
		{
			// Format: 0 "bone_00" -1
			string[] parts = line.Split(new char[] { ' ', '"' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length >= 3)
			{
				Bone bone = new Bone();
				bone.Id = int.Parse(parts[0]);
				bone.Name = parts[1];
				bone.ParentId = int.Parse(parts[2]);
				Bones.Add(bone);
			}
		}

		private void ParseBonePose(string line, NumberFormatInfo nfi)
		{
			// Format: 0  1.234567 2.345678 3.456789  0.123456 0.234567 0.345678
			string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length >= 7)
			{
				int boneId = int.Parse(parts[0]);
				if (boneId < Bones.Count)
				{
					Bones[boneId].Position = new Vector3D(
						float.Parse(parts[1], nfi),
						float.Parse(parts[2], nfi),
						float.Parse(parts[3], nfi)
					);
					Bones[boneId].Rotation = new Vector3D(
						float.Parse(parts[4], nfi),
						float.Parse(parts[5], nfi),
						float.Parse(parts[6], nfi)
					);
				}
			}
		}

		private void ParseTriangle(ref StreamReader sr, string material, NumberFormatInfo nfi)
		{
			Triangle tri = new Triangle();
			tri.Material = material;

			for (int i = 0; i < 3; i++)
			{
				string line = sr.ReadLine();
				if (line == null) return;

				string[] parts = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 9) continue;

				Vertex v = new Vertex();
				v.BoneId = int.Parse(parts[0]);
				v.Position = new Vector3D(
					float.Parse(parts[1], nfi),
					float.Parse(parts[2], nfi),
					float.Parse(parts[3], nfi)
				);
				v.Normal = new Vector3D(
					float.Parse(parts[4], nfi),
					float.Parse(parts[5], nfi),
					float.Parse(parts[6], nfi)
				);
				v.U = float.Parse(parts[7], nfi);
				v.V = float.Parse(parts[8], nfi);

				// Parse bone weights if present
				if (parts.Length > 9)
				{
					int numWeights = int.Parse(parts[9]);
					for (int w = 0; w < numWeights && (10 + w * 2 + 1) < parts.Length; w++)
					{
						BoneWeight bw = new BoneWeight();
						bw.BoneId = int.Parse(parts[10 + w * 2]);
						bw.Weight = float.Parse(parts[10 + w * 2 + 1], nfi);
						v.Weights.Add(bw);
					}
				}

				tri.Vertices[i] = v;
			}

			Triangles.Add(tri);
		}
	}
}

