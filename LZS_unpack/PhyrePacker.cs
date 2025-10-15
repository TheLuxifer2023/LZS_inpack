using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using APPLIB;

namespace LZS_unpack
{
	/// <summary>
	/// Packs SMD and Mesh.ascii files back into Phyre Engine format
	/// </summary>
	internal class PhyrePacker
	{
		private SMDParser smdData;
		private MeshAsciiParser meshData;
		private string outputPath;

		public PhyrePacker(string smdPath, string meshAsciiPath, string outputPath)
		{
			this.outputPath = outputPath;

			// Parse input files
			smdData = new SMDParser();
			smdData.Parse(smdPath);

			meshData = new MeshAsciiParser();
			meshData.Parse(meshAsciiPath);
		}

		public void Pack()
		{
			FileStream fs = new FileStream(outputPath, FileMode.Create);
			BinaryWriter bw = new BinaryWriter(fs);

			Console.WriteLine("Packing Phyre file...");
			Console.WriteLine("Bones: " + smdData.Bones.Count);
			Console.WriteLine("Triangles: " + smdData.Triangles.Count);
			Console.WriteLine("Submeshes: " + meshData.Submeshes.Count);

			// Calculate data sizes and offsets
			int numSubmeshes = meshData.Submeshes.Count;
			int numBones = smdData.Bones.Count;
			int totalVertices = 0;
			int totalIndices = 0;

			foreach (var submesh in meshData.Submeshes)
			{
				totalVertices += submesh.Vertices.Count;
				totalIndices += submesh.Faces.Count * 3;
			}

			// Build class name table
			List<string> classNames = new List<string>();
			classNames.Add("PDataBlock");
			classNames.Add("PMatrix4");
			classNames.Add("PMesh");
			classNames.Add("PMeshSegment");
			classNames.Add("PSkinBoneRemap");
			classNames.Add("PNode");

			// Write file header
			WriteHeader(bw, numSubmeshes, numBones, totalVertices, totalIndices);

			// Write class definitions
			WriteClassDefinitions(bw, classNames);

			// Write object instances
			WriteObjectInstances(bw, numSubmeshes, numBones);

			// Write skeleton data
			WriteSkeletonData(bw);

			// Write mesh data
			WriteMeshData(bw);

			// Write bone remap data
			WriteBoneRemapData(bw, numBones);

			bw.Close();
			fs.Close();

			Console.WriteLine("Successfully packed to: " + outputPath);
		}

		private void WriteHeader(BinaryWriter bw, int numSubmeshes, int numBones, int totalVertices, int totalIndices)
		{
			// This is a simplified header structure based on the unpacker
			// Real Phyre format may have more complex header

			bw.Write(0x50485952); // "PHYR" magic (example)
			bw.Write(0x100); // Header size placeholder
			bw.Write(0); // Data offset placeholder
			bw.Write(numSubmeshes); // Number of mesh segments
			bw.Write(0); // Reserved
			bw.Write(totalVertices * 12); // Vertex data size
			bw.Write(0); // Reserved
			bw.Write(totalIndices * 2); // Index data size
			bw.Write(0); // Reserved
			bw.Write(numBones * 64); // Matrix data size
			bw.Write(0); // Reserved
			bw.Write(0); // Reserved
			bw.Write(0); // String table offset
			bw.Write(0); // String table size
			bw.Write(0); // Reserved
			bw.Write(numSubmeshes); // Mesh segment count
			bw.Write(numBones); // Bone count
			bw.Write(0); // Reserved
			bw.Write(0); // Class definition offset
		}

		private void WriteClassDefinitions(BinaryWriter bw, List<string> classNames)
		{
			// Write class count
			bw.Write(classNames.Count);
			bw.Write(0); // Reserved
			bw.Write(0); // Reserved

			// Write class definition table (simplified)
			foreach (string className in classNames)
			{
				bw.Write(0); // Class ID placeholder
				bw.Write(0); // Property count placeholder
				bw.Write(0); // Name offset
				bw.Write(0); // Property offset
				bw.Write(0); // Reserved
				bw.Write(0); // Reserved
				bw.Write(0); // Reserved
				bw.Write(0); // Reserved
				bw.Write(0); // Reserved
			}

			// Write class name strings
			foreach (string className in classNames)
			{
				foreach (char c in className)
				{
					bw.Write((byte)c);
				}
				bw.Write((byte)0); // Null terminator
			}
		}

		private void WriteObjectInstances(BinaryWriter bw, int numSubmeshes, int numBones)
		{
			// Write object instance count
			bw.Write(numSubmeshes + numBones + 1); // Mesh segments + bones + root node

			// Write PMesh instance
			bw.Write(2); // Class ID (PMesh)
			bw.Write(numSubmeshes); // Segment count
			bw.Write(0); // Data offset placeholder
			for (int i = 0; i < 6; i++) bw.Write(0); // Reserved

			// Write PMeshSegment instances
			for (int i = 0; i < numSubmeshes; i++)
			{
				bw.Write(3); // Class ID (PMeshSegment)
				bw.Write(meshData.Submeshes[i].Vertices.Count);
				bw.Write(0); // Data offset placeholder
				bw.Write(0); // Reserved
				bw.Write(meshData.Submeshes[i].Faces.Count);
				bw.Write(0); // Index offset placeholder
				for (int j = 0; j < 3; j++) bw.Write(0); // Reserved
			}

			// Write PNode instances (skeleton)
			bw.Write(5); // Class ID (PNode)
			bw.Write(0); // Root position X
			bw.Write(0); // Root position Y
			bw.Write(0); // Root position Z
			for (int i = 0; i < 6; i++) bw.Write(0); // Reserved

			// Write PMatrix4 instances (bone matrices)
			for (int i = 0; i < numBones; i++)
			{
				bw.Write(1); // Class ID (PMatrix4)
				bw.Write(0); // Matrix offset placeholder
				for (int j = 0; j < 7; j++) bw.Write(0); // Reserved
			}

			// Write PSkinBoneRemap instances
			for (int i = 0; i < numSubmeshes; i++)
			{
				bw.Write(4); // Class ID (PSkinBoneRemap)
				bw.Write(numBones); // Bone count
				bw.Write(0); // Remap offset placeholder
				for (int j = 0; j < 6; j++) bw.Write(0); // Reserved
			}
		}

		private void WriteSkeletonData(BinaryWriter bw)
		{
			// Write bone matrices
			foreach (var bone in smdData.Bones)
			{
				// Convert Euler angles to quaternion, then to matrix
				Quaternion3D quat = C3D.EulerAnglesToQuaternion(
					bone.Rotation.X,
					bone.Rotation.Y,
					bone.Rotation.Z
				);

				// Write 4x4 matrix (simplified - identity with position)
				bw.Write(1.0f); bw.Write(0.0f); bw.Write(0.0f); bw.Write(bone.Position.X);
				bw.Write(0.0f); bw.Write(1.0f); bw.Write(0.0f); bw.Write(bone.Position.Y);
				bw.Write(0.0f); bw.Write(0.0f); bw.Write(1.0f); bw.Write(bone.Position.Z);
				bw.Write(0.0f); bw.Write(0.0f); bw.Write(0.0f); bw.Write(1.0f);
			}
		}

		private void WriteMeshData(BinaryWriter bw)
		{
			// Write vertex and index data for each submesh
			foreach (var submesh in meshData.Submeshes)
			{
				// Write vertices
				foreach (var vertex in submesh.Vertices)
				{
					bw.Write(vertex.X);
					bw.Write(vertex.Y);
					bw.Write(vertex.Z);
				}

				// Write faces as indices
				foreach (var face in submesh.Faces)
				{
					bw.Write((ushort)face[0]);
					bw.Write((ushort)face[1]);
					bw.Write((ushort)face[2]);
				}
			}
		}

		private void WriteBoneRemapData(BinaryWriter bw, int numBones)
		{
			// Write bone remap table (identity mapping)
			for (int i = 0; i < numBones; i++)
			{
				bw.Write((ushort)i);
				bw.Write((ushort)0); // Padding
			}
		}
	}
}

