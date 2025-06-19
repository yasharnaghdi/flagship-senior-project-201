# Terminal Coach VM Installation Guide

This guide explains how to set up Terminal Coach as a bootable virtual machine on a separate partition that functions as an ejectable drive rather than a standard application.

## Prerequisites

- macOS 10.13 or higher
- At least 20GB of free disk space
- Administrator privileges
- Basic familiarity with Terminal commands
- Virtual machine software (VirtualBox or VMware Fusion)

## Step 1: Create a Dedicated Partition

First, we'll create a separate partition on your hard drive for Terminal Coach.

1. Open **Disk Utility** from Applications > Utilities.

2. Select your main disk in the sidebar and click **Partition**.

3. Click the "+" button to add a new partition.

4. Set the following parameters:
   - Name: `TerminalCoach`
   - Format: APFS (or Mac OS Extended if using an older macOS)
   - Size: 10-20GB (depending on your available space)

5. Click **Apply** to create the partition.

## Step 2: Set Up the Virtual Machine Image

1. Install Virtual Machine software if you haven't already:
   ```bash
   brew install --cask virtualbox
   # OR
   brew install --cask vmware-fusion
   ```

2. Download the .NET Core SDK ISO from the official Microsoft site:
   ```bash
   curl -O https://download.visualstudio.microsoft.com/download/pr/e35ad60e-1e51-43ee-bd6c-6b90df977b54/c785f44ed683a83b4f399bb800da7974/dotnet-sdk-6.0.400-osx-x64.pkg
   ```

3. Install the .NET SDK:
   ```bash
   sudo installer -pkg dotnet-sdk-6.0.400-osx-x64.pkg -target /
   ```

4. Create a new virtual machine with a minimal Linux distribution (Ubuntu Server is recommended).

5. When prompted for storage location, choose the `TerminalCoach` partition you created earlier.

## Step 3: Configure the Virtual Machine as an Ejectable Drive

1. Open Terminal and create a disk image of your virtual machine:
   ```bash
   cd /Volumes/TerminalCoach
   hdiutil create -size 10g -fs APFS -volname "TerminalCoach" TerminalCoach.dmg
   ```

2. Mount the disk image:
   ```bash
   hdiutil attach TerminalCoach.dmg -mountpoint /Volumes/TerminalCoachVM
   ```

3. Move the virtual machine files to the mounted disk image:
   ```bash
   mv YourVM.vmwarevm /Volumes/TerminalCoachVM/
   # OR for VirtualBox
   mv YourVM /Volumes/TerminalCoachVM/
   ```

4. Create an autorun script to launch the VM automatically:
   ```bash
   cat > /Volumes/TerminalCoachVM/autorun.sh << 'EOL'
   #!/bin/bash
   # Detect VM software
   if [ -d "/Applications/VMware Fusion.app" ]; then
     "/Applications/VMware Fusion.app/Contents/MacOS/VMware Fusion" "$( dirname "$0" )/YourVM.vmwarevm"
   elif [ -d "/Applications/VirtualBox.app" ]; then
     "/Applications/VirtualBox.app/Contents/MacOS/VirtualBox" "$( dirname "$0" )/YourVM/YourVM.vbox"
   else
     osascript -e 'display dialog "Please install VMware Fusion or VirtualBox to run Terminal Coach VM" buttons {"OK"} default button "OK"'
   fi
   EOL
   
   chmod +x /Volumes/TerminalCoachVM/autorun.sh
   ```

5. Create a launcher application using Automator:
   - Open **Automator**
   - Create a new **Application**
   - Add a **Run Shell Script** action
   - Enter the following script:
     ```bash
     /Volumes/TerminalCoachVM/autorun.sh
     ```
   - Save it as `TerminalCoach.app` in the mounted disk image

6. Unmount the disk image:
   ```bash
   hdiutil detach /Volumes/TerminalCoachVM
   ```

## Step 4: Install Terminal Coach in the Virtual Machine

1. Mount the disk image:
   ```bash
   hdiutil attach /Volumes/TerminalCoach/TerminalCoach.dmg
   ```

2. Run the launcher application to start the VM.

3. Once the VM boots, open a terminal in the Linux VM and install the necessary prerequisites:
   ```bash
   # Update package lists
   sudo apt update
   
   # Install .NET Core on Linux
   wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   sudo apt update
   sudo apt install -y dotnet-sdk-6.0
   
   # Install git
   sudo apt install -y git
   ```

4. Clone the Terminal Coach repository:
   ```bash
   git clone https://github.com/username/terminal-coach.git
   cd terminal-coach
   ```

5. Build and run Terminal Coach:
   ```bash
   dotnet build
   dotnet run
   ```

## Step 5: Create a Make it Ejectable

To make the VM truly ejectable like a USB drive:

1. Open Terminal on your Mac and convert the disk image to a sparse bundle:
   ```bash
   hdiutil convert /Volumes/TerminalCoach/TerminalCoach.dmg -format UDSB -o /Volumes/TerminalCoach/TerminalCoach.sparsebundle
   rm /Volumes/TerminalCoach/TerminalCoach.dmg
   ```

2. Create an eject script:
   ```bash
   cat > /Volumes/TerminalCoach/eject.sh << 'EOL'
   #!/bin/bash
   
   # Safely shut down any running VM
   if pgrep -f "VMware Fusion" > /dev/null; then
     osascript -e 'tell application "VMware Fusion" to quit'
   elif pgrep -f "VirtualBox" > /dev/null; then
     osascript -e 'tell application "VirtualBox" to quit'
   fi
   
   # Wait for VM to shut down
   sleep 5
   
   # Detach the disk image
   hdiutil detach /Volumes/TerminalCoachVM 2>/dev/null
   
   # Unmount the TerminalCoach partition
   diskutil unmount /Volumes/TerminalCoach
   
   osascript -e 'display dialog "Terminal Coach has been safely ejected" buttons {"OK"} default button "OK"'
   EOL
   
   chmod +x /Volumes/TerminalCoach/eject.sh
   ```

3. Create an eject application using Automator:
   - Open **Automator**
   - Create a new **Application**
   - Add a **Run Shell Script** action
   - Enter the following script:
     ```bash
     /Volumes/TerminalCoach/eject.sh
     ```
   - Save it as `Eject TerminalCoach.app` on the TerminalCoach volume

## Usage Instructions

### Mounting the Terminal Coach VM

1. Open **Disk Utility**
2. Select the TerminalCoach partition
3. Click **Mount** if it's not already mounted
4. Double-click on the TerminalCoach.sparsebundle to mount the VM disk image
5. Launch Terminal Coach by double-clicking the TerminalCoach.app

### Ejecting the Terminal Coach VM

1. Make sure you've saved any work in the VM
2. Double-click the "Eject TerminalCoach.app" 
3. Wait for the confirmation dialog

## Troubleshooting

### VM Won't Start

Check if the VM disk image is properly mounted:
```bash
hdiutil attach /Volumes/TerminalCoach/TerminalCoach.sparsebundle -mountpoint /Volumes/TerminalCoachVM
```

### Can't Eject the Drive

Force eject if the normal method fails:
```bash
sudo diskutil unmountDisk force /dev/disk#  # Replace # with the appropriate disk number
```

### Finding the Disk Number

If you need to find the correct disk number for troubleshooting:
```bash
diskutil list
```

Look for the disk with the name "TerminalCoach"

## Maintenance

Periodically check for updates to the Terminal Coach repository:

```bash
# Inside the VM
cd terminal-coach
git pull
dotnet build
```

---

This configuration allows you to use Terminal Coach as if it were an external drive that you can mount and eject as needed, while keeping it completely separate from your main system.
