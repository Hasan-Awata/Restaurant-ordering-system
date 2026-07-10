import os
import re
from collections import deque

# ==========================================
# CONFIGURATION
# ==========================================

PROJECT_ROOT = r"C:\Users\ASUS\source\repos\OrderingSystem\Backend"
OUTPUT_FILE = "Feature_Flow_Dump.txt"

# The entry points of the feature (The script will trace downwards from here)
ENTRY_FILES = ["TablesController.cs"]

IGNORE_DIRS = {
    ".git", ".vs", "bin", "obj", "node_modules", 
    "Migrations", "Properties", "TestResults", ".idea"
}

IGNORE_EXTENSIONS = {
    ".dll", ".exe", ".png", ".jpg", ".jpeg", ".pdb", 
    ".suo", ".user", ".cache", ".sln", ".json"
}

# ==========================================
# CORE LOGIC
# ==========================================

def clean_csharp_content(content):
    """Strips standard boilerplate 'using' statements to save AI tokens."""
    cleaned_lines = []
    for line in content.split('\n'):
        # Keep namespace declarations, strip basic usings
        if line.strip().startswith("using ") and not line.strip().startswith("using ("):
            continue
        cleaned_lines.append(line)
    return '\n'.join(cleaned_lines).strip()

def build_project_roster():
    """
    Scans the project and creates a map of ClassName -> FilePath.
    Automatically filters out Interfaces and Mappers.
    """
    roster = {}
    domain_files = []
    db_context_file = None

    for root, dirs, files in os.walk(PROJECT_ROOT):
        # Mutate dirs in-place to skip ignored directories
        dirs[:] = [d for d in dirs if d not in IGNORE_DIRS]

        path_parts = root.split(os.sep)
        
        # Rule: Filter out Interfaces and Mappers entirely
        if "Interfaces" in path_parts or "Mappers" in path_parts:
            continue

        for file in files:
            if any(file.endswith(ext) for ext in IGNORE_EXTENSIONS):
                continue
                
            if file.endswith(".cs"):
                class_name = file[:-3] # Remove .cs extension
                full_path = os.path.join(root, file)
                roster[class_name] = full_path

                # Rule: Isolate Domain layer and DbContext for mandatory inclusion
                if "OrderingSystem.Domain" in path_parts:
                    domain_files.append(full_path)
                if class_name == "OrderingSystemDbContext":
                    db_context_file = full_path

    return roster, domain_files, db_context_file

def find_references_in_code(content, roster):
    """
    Scans C# code for words that match known classes in our project.
    If it finds an Interface (e.g., ISessionCommandService), it looks for the concrete class.
    """
    references = set()
    # Find all Capitalized words (standard C# class/interface naming convention)
    words = set(re.findall(r'\b[A-Z][a-zA-Z0-9_]+\b', content))
    
    for word in words:
        # If the word directly matches a known class (e.g., ProcessQrCodeRequest)
        if word in roster:
            references.add(roster[word])
            
        # If the word is an interface (e.g., ISessionCommandService), strip the 'I' and look for the implementation
        elif word.startswith("I") and len(word) > 1 and word[1].isupper():
            concrete_class = word[1:]
            if concrete_class in roster:
                references.add(roster[concrete_class])
                
    return references

def generate_dump():
    print("Building project architecture roster...")
    roster, domain_files, db_context_file = build_project_roster()
    
    # The investigation board: keeps track of what we've already processed
    evidence_board = set()
    
    # The queue of leads to follow
    leads = deque()

    # 1. Add mandatory files to the evidence board first
    for d_file in domain_files:
        evidence_board.add(d_file)
    if db_context_file:
        evidence_board.add(db_context_file)

    # 2. Add entry points to the leads queue
    for entry in ENTRY_FILES:
        class_name = entry.replace(".cs", "")
        if class_name in roster:
            leads.append(roster[class_name])
        else:
            print(f"WARNING: Entry point {entry} not found in project.")

    processed_files = []

    print("Tracing execution flow...")
    
    # 3. Process the leads recursively
    while leads:
        current_file = leads.popleft()
        
        if current_file in evidence_board and current_file not in domain_files:
            continue # Already processed (unless it's a domain file we pre-loaded but haven't scraped for leads)
            
        evidence_board.add(current_file)
        
        try:
            with open(current_file, "r", encoding="utf-8-sig") as infile:
                content = infile.read()
                
            processed_files.append((current_file, content))
            
            # Extract new leads from this file and add them to the queue
            new_leads = find_references_in_code(content, roster)
            for lead in new_leads:
                if lead not in evidence_board and lead not in leads:
                    leads.append(lead)
                    
        except Exception as e:
            print(f"Error reading {current_file}: {e}")

    # 4. Write everything to the dump file
    print("Compiling final dump...")
    with open(OUTPUT_FILE, "w", encoding="utf-8-sig") as outfile:
        outfile.write("DEPENDENCY TRACER DUMP\n")
        outfile.write(f"ENTRY POINTS: {', '.join(ENTRY_FILES)}\n")
        outfile.write("RULES: Domain Included, DbContext Included, Interfaces/Mappers Excluded.\n")
        outfile.write("=" * 60 + "\n\n")

        # Write Domain and DbContext first for structural context
        mandatory_paths = set(domain_files)
        if db_context_file:
            mandatory_paths.add(db_context_file)
            
        # Write traced files
        for file_path, content in processed_files:
            outfile.write(f"FILE: {file_path}\n")
            outfile.write("-" * 40 + "\n")
            outfile.write(clean_csharp_content(content) + "\n\n\n")

        # Write mandatory files if they weren't organically hit by the tracer
        for m_path in mandatory_paths:
            if m_path not in [p[0] for p in processed_files]:
                try:
                    with open(m_path, "r", encoding="utf-8-sig") as infile:
                        outfile.write(f"FILE: {m_path}\n")
                        outfile.write("-" * 40 + "\n")
                        outfile.write(clean_csharp_content(infile.read()) + "\n\n\n")
                except Exception:
                    pass

        outfile.write("\n" + "=" * 60 + "\n")
        outfile.write(f"END OF DUMP. Total files analyzed: {len(evidence_board)}\n")
        
    print(f"\nTrace complete. Successfully mapped {len(evidence_board)} files into '{OUTPUT_FILE}'.")

if __name__ == "__main__":
    generate_dump()