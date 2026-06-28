import os

def gather_project_code(root_dir, output_file):
    # Folders to ignore so we don't dump binaries or git history
    ignore_dirs = {'.git', '.vs', 'bin', 'obj', 'node_modules', 'Database', 'Docs', 'Flutter'}
    
    # File extensions to include
    allowed_extensions = {'.cs', '.json', '.csproj', '.sql'}
    
    # Initialize our counter
    successful_files = 0

    print(f"Scanning directory: {root_dir}...\n")

    with open(output_file, 'w', encoding='utf-8') as outfile:
        output_file_name = os.path.basename(output_file)
        
        outfile.write("PROJECT ARCHITECTURE SNAPSHOT\n")
        outfile.write("=============================\n\n")

        for dirpath, dirnames, filenames in os.walk(root_dir):
            # Modify dirnames in-place to skip ignored directories
            dirnames[:] = [d for d in dirnames if d not in ignore_dirs]

            for filename in filenames:
                # Skip the output file itself so we don't create an infinite loop
                if filename == output_file_name:
                    continue
                    
                # Check if the file has an allowed extension
                if any(filename.endswith(ext) for ext in allowed_extensions):
                    filepath = os.path.join(dirpath, filename)
                    
                    try:
                        with open(filepath, 'r', encoding='utf-8') as infile:
                            content = infile.read()
                            
                        # Write the file header
                        outfile.write(f"\nFILE: {filepath}\n")
                        outfile.write("-" * 40 + "\n")
                        # Write the file content
                        outfile.write(content)
                        outfile.write("\n\n")
                        
                        # Print the success message to the console and increment the counter
                        print(f"[SUCCESS] Dumped: {filepath}")
                        successful_files += 1
                        
                    except Exception as e:
                        print(f"[ERROR] Could not read {filepath}: {e}")

    # Print the final summary report
    print("\n" + "="*50)
    print("DUMP COMPLETE!")
    print(f"Total files successfully dumped: {successful_files}")
    print(f"Output saved to: {output_file}")
    print("="*50)

if __name__ == "__main__":
    # Make sure this points to your new target directory!
    project_root = r"C:\Users\ASUS\source\repos\OrderingSystem\Backend" 
    
    # The name of the text file you want to generate
    output_filename = "lean_project_dump.txt"
    
    gather_project_code(project_root, output_filename)