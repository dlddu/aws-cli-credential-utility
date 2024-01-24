## 귀찮았던 부분
aws sso login을 통해 발급받은 credential은 테라폼의 s3 백엔드에는 적용이 안된다
## 사용 환경
- identity center portal에서 발급받은 short term credential을 %userprofile%\.aws\credentials에 붙여넣어서 사용
- %userprofile%\.aws\config 파일에서 source_profile + role_arn을 통해 프로파일 사용
## 작동 방식
1. C#의 Process로 aws sso login --profile {profile-name} 실행
2. %userprofile%\.aws\sso\cache 폴더 아래에 마지막으로 수정된 json 파일에 들어있는 token을 읽음
3. 해당 토큰으로 short term credential을 발급받음
4. %userprofile%\.aws\credentials 파일 수정해서 저장
