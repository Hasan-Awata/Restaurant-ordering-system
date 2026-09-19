import os
import re

def generate_tree(dir_path, ignore_dirs, prefix=""):
    """Generates an ASCII directory tree for the LLM table of contents."""
    tree_str = ""
    try:
        entries = sorted(os.listdir(dir_path))
    except PermissionError:
        return ""
    
    entries = [e for e in entries if e not in ignore_dirs and not e.startswith('.')]
    entries_count = len(entries)
    
    for index, entry in enumerate(entries):
        path = os.path.join(dir_path, entry)
        connector = "└── " if index == entries_count - 1 else "├── "
        tree_str += f"{prefix}{connector}{entry}\n"
        
        if os.path.isdir(path):
            extension = "    " if index == entries_count - 1 else "│   "
            tree_str += generate_tree(path, ignore_dirs, prefix + extension)
            
    return tree_str

def clean_code(content):
    """Reduces empty lines and redundant whitespace to save tokens."""
    content = re.sub(r'\n\s*\n\s*\n+', '\n\n', content)
    content = "\n".join(line.rstrip() for line in content.splitlines())
    return content.strip()

def gather_project_code(root_dir, output_file, include_migrations=False):
    ignore_dirs = {
        '.git', '.vs', 'bin', 'obj', 'node_modules', 
        'Database', 'Docs', 'Flutter', 'TestResults', '.csproj.user'
    }
    
    if not include_migrations:
        ignore_dirs.update({'Migrations'})

    allowed_extensions = {'.cs', '.json', '.csproj', '.sql'}
    
    # Common .NET auto-generated patterns that bloat context
    ignored_patterns = {'.Designer.cs', '.generated.cs', '.assemblyinfo.cs'}
    
    successful_files = 0
    total_chars = 0
    skipped_log = []
    
    output_file_name = os.path.basename(output_file)
    print(f"Scanning .NET project at: {root_dir}...\n")

    with open(output_file, 'w', encoding='utf-8') as outfile:
        
        # Write .NET specific system directives for the LLM
        outfile.write("<system_directives>\n")
        outfile.write("  <rule>Read this entire document carefully. Do NOT assume code is missing. Write complete, fully functional blocks.</rule>\n")
        outfile.write("  <rule>Strictly adhere to Clean Architecture, SOLID principles, and Entity Framework Core best practices.</rule>\n")
        outfile.write("  <rule>Rely ONLY on the explicit DTOs, interfaces, and service registrations provided in this context. Do not hallucinate external libraries or dependencies.</rule>\n")
        outfile.write("  <rule>When proposing backend fixes, ensure proper async/await patterns and dependency injection lifetimes (Transient/Scoped/Singleton) are respected.</rule>\n")
        outfile.write("  <rule>If the fix requires less than three modifications within the same file, don't rewrite the whole file; indicate precisely where to apply changes.</rule>\n")
        outfile.write("</system_directives>\n\n")

        # Write directory tree structure
        outfile.write("<project_context>\n")
        outfile.write(f"  <root_directory>{os.path.basename(os.path.abspath(root_dir))}</root_directory>\n")
        outfile.write("  <directory_structure>\n")
        outfile.write(generate_tree(root_dir, ignore_dirs))
        outfile.write("  </directory_structure>\n")
        outfile.write("</project_context>\n\n")

        outfile.write("<source_code>\n")
        
        for dirpath, dirnames, filenames in os.walk(root_dir):
            dirnames[:] = [d for d in dirnames if d not in ignore_dirs and not d.startswith('.')]

            for filename in sorted(filenames):
                filepath = os.path.join(dirpath, filename)
                relative_path = os.path.relpath(filepath, root_dir).replace("\\", "/")

                # Skip output file if it's nested inside the scanned folder
                if filename == output_file_name:
                    skipped_log.append(f"[OUTPUT FILE] {relative_path}")
                    continue

                if any(pattern.lower() in filename.lower() for pattern in ignored_patterns):
                    skipped_log.append(f"[IGNORED PATTERN] {relative_path}")
                    continue

                is_allowed_ext = any(filename.endswith(ext) for ext in allowed_extensions)
                if not is_allowed_ext:
                    skipped_log.append(f"[UNALLOWED EXT] {relative_path}")
                    continue
                    
                # Skip files larger than 100KB to protect context window limit
                if os.path.getsize(filepath) > 100 * 1024:
                    skipped_log.append(f"[EXCEEDS 100KB] {relative_path}")
                    continue
                
                try:
                    with open(filepath, 'r', encoding='utf-8') as infile:
                        content = infile.read()
                        
                    cleaned_content = clean_code(content)
                    
                    outfile.write(f'<file path="{relative_path}">\n')
                    outfile.write(cleaned_content)
                    outfile.write("\n</file>\n\n")
                    
                    successful_files += 1
                    total_chars += len(cleaned_content)
                    print(f"[PACKED] {relative_path}")
                    
                except Exception as e:
                    print(f"[ERROR] Could not read {relative_path}: {e}")

        outfile.write("</source_code>\n")

    # Output skipped files log directly to the terminal
    print("\n" + "-" * 50)
    print("SKIPPED FILES LOG")
    print("-" * 50)
    if skipped_log:
        for log in skipped_log:
            print(log)
    else:
        print("No files were skipped.")

    estimated_tokens = total_chars // 4
    print("\n" + "=" * 50)
    print("AI CONTEXT DUMP COMPLETE!")
    print(f"Files Packed: {successful_files}")
    print(f"Estimated Tokens: ~{estimated_tokens:,}")
    print(f"Main Output: {output_file}")
    print("=" * 50)

if __name__ == "__main__":
    project_root = r"C:\MyFile\Project\OrderingSystem\Backend" 
    output_filename = "ai_net_project_context.xml"
    
    # Toggle `include_migrations=True` if you need Entity Framework migrations in context
    gather_project_code(
        root_dir=project_root, 
        output_file=output_filename, 
        include_migrations=False
    )