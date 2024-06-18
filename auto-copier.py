from dotenv import dotenv_values
from zipfile import ZipFile

def main():
    env = dotenv_values(".env")
    
    path = env["project_name"] + "\\bin\\Debug\\net7.0"
    files = env["files"].split(",")
    mod_folder = env["mod_folder"]
    
    print(path)
    print(str(files))
    print(mod_folder)
    
    with ZipFile(mod_folder + "\\" + env["project_name"] + ".zip", 'w') as zip:
        for filename in files:
            zip.write(path + "\\" + filename, filename)
    
main()